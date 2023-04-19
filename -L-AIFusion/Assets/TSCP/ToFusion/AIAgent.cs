using Fusion;
using Fusion.KCC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class AIAgent : Agent, IBeforeUpdate, IBeforeTick
    {
        public GameplayInput _lastKnownInput;

        private GameplayInput _fixedInput;
        private GameplayInput _renderInput;
        private GameplayInput _cachedInput;
        private GameplayInput _baseFixedInput;
        private GameplayInput _baseRenderInput;
        private Vector2 _cachedMoveDirection;
        private float _cachedMoveDirectionSize;
        private bool _resetCachedInput;
        private int _missingInputsTotal;
        private int _missingInputsInRow;
        private int _logMissingInputFromTick;
        private FrameRecord[] _frameRecords = new FrameRecord[128];

        private float _grenadesCyclingStartTime;
        public override void Spawned()
        {
            base.Spawned();
            _fixedInput = default;
            _renderInput = default;
            _cachedInput = default;
            _lastKnownInput = default;
            _baseFixedInput = default;
            _baseRenderInput = default;
        }
        public override void SetInputDirection(Vector3 direction)
        {
            _lastKnownInput.MoveDirection = direction;
            _fixedInput.MoveDirection = direction;

        }
        public override void SetFixedInput(GameplayInput fixedInput, bool updateBaseInputs)
        {
            CheckFixedAccess(true);

            _fixedInput = fixedInput;

            if (updateBaseInputs == true)
            {
                _baseFixedInput = fixedInput;
                _baseRenderInput = fixedInput;
            }
        }

        public bool WasActivated(EGameplayInputAction action)
        {
            if (Runner.Stage != default)
            {
                CheckFixedAccess(false);
                return action.WasActivated(_fixedInput, _baseFixedInput);
            }
            else
            {
                CheckRenderAccess(false);
                return action.WasActivated(_renderInput, _baseRenderInput);
            }
        }

        public void SetRenderInput(GameplayInput renderInput, bool updateBaseInput)
        {
            CheckRenderAccess(false);

            _renderInput = renderInput;

            if (updateBaseInput == true)
            {
                _baseRenderInput = renderInput;
            }
        }

        public bool WasActivated(EGameplayInputAction action, GameplayInput customInput)
        {
            if (Runner.Stage != default)
            {
                CheckFixedAccess(false);
                return action.WasActivated(customInput, _baseFixedInput);
            }
            else
            {
                CheckRenderAccess(false);
                return action.WasActivated(customInput, _baseRenderInput);
            }
        }

        protected override GameplayInput GetFixedInput()
        {
            return _fixedInput;
        }
        protected override void ProcessFixedInput()
        {
            KCC kcc = _character.CharacterController;
            KCCData kccFixedData = kcc.FixedData;

            GameplayInput input = default;

            if (_health.IsAlive == true)
            {
                input = GetFixedInput();
            }

            if (input.Aim == true)
            {
                input.Aim &= CanAim(kccFixedData);
            }

            if (input.Aim == true)
            {
                if (_weapons.CurrentWeapon != null && _weapons.CurrentWeapon.HitType == EHitType.Sniper)
                {
                    input.LookRotationDelta *= 0.3f;
                }
            }

            kcc.SetAim(input.Aim);

            if (WasActivated(EGameplayInputAction.Jump, input) == true && _character.AnimationController.CanJump() == true)
            {
                kcc.Jump(Vector3.up * _jumpPower);
            }

            SetLookRotation(kccFixedData, input.LookRotationDelta, _weapons.GetRecoil(), out Vector2 newRecoil);
            _weapons.SetRecoil(newRecoil);

            kcc.SetInputDirection(input.MoveDirection.IsZero() == true ? Vector3.zero : kcc.FixedData.TransformRotation * input.MoveDirection.X0Y());

            if (WasActivated(EGameplayInputAction.ToggleSide, input) == true)
            {
                LeftSide = !LeftSide;
            }

            if (WasActivated(EGameplayInputAction.ToggleSpeed, input) == true)
            {
                if (kcc.HasModifier(_fastMovementProcessor) == true)
                {
                    kcc.RemoveModifier(_fastMovementProcessor);
                }
                else
                {
                    kcc.AddModifier(_fastMovementProcessor);
                }
            }

            if (input.Weapon > 0 && _character.AnimationController.CanSwitchWeapons(true) == true && _weapons.SwitchWeapon(input.Weapon - 1) == true)
            {
                _character.AnimationController.SwitchWeapons();
            }
            else if (input.Weapon <= 0 && _weapons.PendingWeaponSlot != _weapons.CurrentWeaponSlot && _character.AnimationController.CanSwitchWeapons(false) == true)
            {
                _character.AnimationController.SwitchWeapons();
            }

            if (WasActivated(EGameplayInputAction.ToggleJetpack, input) == true)
            {
                if (_jetpack.IsActive == true)
                {
                    _jetpack.Deactivate();
                }
                else if (_character.AnimationController.CanSwitchWeapons(true) == true)
                {
                    _jetpack.Activate();
                }
            }

            if (_jetpack.IsActive == true)
            {
                _jetpack.FullThrust = input.Thrust;
            }

            SetFixedInput(input, false);
        }

        protected override void ProcessRenderInput()
        {
            if (Object.HasInputAuthority == false)
                return;

            KCC kcc = _character.CharacterController;
            KCCData kccFixedData = kcc.FixedData;

            GameplayInput input = default;

            if (_health.IsAlive == true)
            {
                input = _renderInput;

                var cachedInput = _cachedInput;

                input.LookRotationDelta = cachedInput.LookRotationDelta;
                input.Aim = cachedInput.Aim;
                input.Thrust = cachedInput.Thrust;
            }

            if (input.Aim == true)
            {
                input.Aim &= CanAim(kccFixedData);
            }

            if (input.Aim == true)
            {
                if (_weapons.CurrentWeapon != null && _weapons.CurrentWeapon.HitType == EHitType.Sniper)
                {
                    input.LookRotationDelta *= 0.3f;
                }
            }

            SetLookRotation(kccFixedData, input.LookRotationDelta, _weapons.GetRecoil(), out Vector2 newRecoil);

            kcc.SetInputDirection(input.MoveDirection.IsZero() == true ? Vector3.zero : kcc.RenderData.TransformRotation * input.MoveDirection.X0Y());

            kcc.SetAim(input.Aim);

            if (WasActivated(EGameplayInputAction.Jump, input) == true && _character.AnimationController.CanJump() == true)
            {
                kcc.Jump(Vector3.up * _jumpPower);
            }
        }

        public override void Render()
        {
            _fixedInput = default;
            _renderInput = default;
            _cachedInput = default;
            _lastKnownInput = default;
            _baseFixedInput = default;
            _baseRenderInput = default;
            base.Render();
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        protected virtual void CheckFixedAccess(bool checkStage)
        {
            return;

            if (checkStage == true && Runner.Stage == default)
            {
                throw new InvalidOperationException("This call should be executed from FixedUpdateNetwork!");
            }

            if (Runner.Stage != default && Object.IsProxy == true)
            {
                throw new InvalidOperationException("Fixed input is available only on State & Input authority!");
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void CheckRenderAccess(bool checkStage)
        {
            return;

            if (checkStage == true && Runner.Stage != default)
            {
                throw new InvalidOperationException("This call should be executed outside of FixedUpdateNetwork!");
            }

            if (Runner.Stage == default && Object.HasInputAuthority == false)
            {
                throw new InvalidOperationException("Render and cached inputs are available only on Input authority!");
            }
        }


        void IBeforeUpdate.BeforeUpdate()
        {
            // Store last render input as a base to current render input.
            _baseRenderInput = _renderInput;

            // Reset input for current frame to default.
            _renderInput = default;

            // Cached input was polled and explicit reset requested.
            if (_resetCachedInput == true)
            {
                _resetCachedInput = false;

                _cachedInput = default;
                _cachedMoveDirection = default;
                _cachedMoveDirectionSize = default;
            }

            return;

            if ((Context.Input.IsCursorVisible == true && Context.Settings.SimulateMobileInput == false) || Context.GameplayMode.State != GameplayMode.EState.Active)
                return;

            Vector2 moveDirection;
            Vector2 lookRotationDelta;

            // Process cached input for next OnInput() call, represents accumulated inputs for all render frames since last fixed update.

            float deltaTime = Time.deltaTime;

            // Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
            // If the player hits W key in last frame before fixed update, the KCC will move in render update by (velocity * 0.003333f).
            // Treating this input the same way for next fixed update results in KCC moving by (velocity * 0.03333f) - 10x more.
            // Following accumulation proportionally scales move direction so it reflects frames in which input was active.
            // This way the next fixed update will correspond more accurately to what happened in render frames.

            _cachedMoveDirection += moveDirection * deltaTime;
            _cachedMoveDirectionSize += deltaTime;

            _cachedInput.Actions = new NetworkButtons(_cachedInput.Actions.Bits | _renderInput.Actions.Bits);
            _cachedInput.MoveDirection = _cachedMoveDirection / _cachedMoveDirectionSize;
            _cachedInput.LookRotationDelta += _renderInput.LookRotationDelta;

            if (_renderInput.Weapon != default)
            {
                _cachedInput.Weapon = _renderInput.Weapon;
            }
        }

        /// <summary>
        /// 3. Read input from Fusion. On input authority the FixedInput will match CachedInput.
        /// </summary>
        void IBeforeTick.BeforeTick()
        {
            // Store last known fixed input. This will be compared agaisnt new fixed input.
            _baseFixedInput = _lastKnownInput;

            // Set fixed input to last known fixed input as a fallback.
            _fixedInput = _lastKnownInput;
            // The current fixed input will be used as a base to first Render after FUN.
            _baseRenderInput = _fixedInput;
        }

    }

}
