using Fusion;
using Fusion.KCC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class AIAgent : Agent
    {
        private float _grenadesCyclingStartTime;
        protected AIAgentInput AIInput => AgentInput as AIAgentInput;

        public override void SetInputAttack()
        {
            Debug.LogError("Attack");

            var input = this.AIInput.GetInput();
            AIInput.GetInput().Attack = true;
        }
        public override void SetRotationDeltaDirect(float pitch, float yaw)
        {
            var input = this.AIInput.GetInput();
            this.AIInput.GetInput().LookRotationDelta += new Vector2(pitch, yaw);
            // AIInput.SetLastKnownInput(input, false);
            //_lastKnownInput.LookRotationDelta +=
            //_fixedInput.LookRotationDelta += new Vector2(pitch, yaw);
        }
        public override void SetInputDirection(Vector3 direction)
        {
            var input = this.AIInput.GetInput();
            this.AIInput.GetInput().MoveDirection = direction;
            // Debug.LogError(input.MoveDirection);
            // AIInput.SetLastKnownInput(input, false);
            //_lastKnownInput.MoveDirection = direction;
            //_fixedInput.MoveDirection = direction;
        }
        public override void SetFixedInput(GameplayInput fixedInput, bool updateBaseInputs)
        {
            CheckFixedAccess(true);
            AIInput.SetFixedInput(fixedInput, updateBaseInputs);

            //_fixedInput = fixedInput;
            //Debug.LogError("SetFixedInput " + _fixedInput.Attack);

            //if (updateBaseInputs == true)
            //{
            //    _baseFixedInput = fixedInput;
            //    _baseRenderInput = fixedInput;
            //}
        }

        public bool WasActivated(EGameplayInputAction action)
        {
            return AIInput.WasActivated(action);

            //if (Runner.Stage != default)
            //{
            //    CheckFixedAccess(false);
            //    return action.WasActivated(_fixedInput, _baseFixedInput);
            //}
            //else
            //{
            //    CheckRenderAccess(false);
            //    return action.WasActivated(_renderInput, _baseRenderInput);
            //}
        }

        public bool WasActivated(EGameplayInputAction action, GameplayInput customInput)
        {
            return AIInput.WasActivated(action, customInput);
            //if (Runner.Stage != default)
            //{
            //    CheckFixedAccess(false);
            //    return action.WasActivated(customInput, _baseFixedInput);
            //}
            //else
            //{
            //    CheckRenderAccess(false);
            //    return action.WasActivated(customInput, _baseRenderInput);
            //}
        }
        public void SetLastKnownInput(GameplayInput fixedInput, bool updateBaseInputs)
        {
            AIInput.SetLastKnownInput(fixedInput, updateBaseInputs);
            //_lastKnownInput = fixedInput;

            //if (updateBaseInputs == true)
            //{
            //    _baseFixedInput = fixedInput;
            //    _baseRenderInput = fixedInput;
            //}
        }
        //protected override GameplayInput GetFixedInput()
        //{
        //    return AIInput.FixedInput;
        //}

        //protected override void OnLateFixedUpdate()
        //{
        //    if (_networkCulling.IsCulled == true)
        //        return;

        //    if (Object.IsProxy == false
        //        && _health.IsAlive == true)
        //    {

        //        bool attackWasActivated = WasActivated(EGameplayInputAction.Attack);
        //        bool reloadWasActivated = WasActivated(EGameplayInputAction.Reload);
        //        bool interactWasActivated = WasActivated(EGameplayInputAction.Interact);
        //        var fixedInputData = GetFixedInput();

        //        TryFire(attackWasActivated, fixedInputData.Attack);
        //        TryReload(reloadWasActivated == false);
        //        _weapons.TryInteract(interactWasActivated, fixedInputData.Interact);
        //    }

        //    _weapons.OnLateFixedUpdate();
        //    _health.OnFixedUpdate();

        //    if (Object.IsProxy == false)
        //    {
        //        SetLastKnownInput(GetFixedInput(), true);
        //    }
        //}

        //protected override void ProcessFixedInput()
        //{
        //    KCC kcc = _character.CharacterController;
        //    KCCData kccFixedData = kcc.FixedData;

        //    GameplayInput input = default;

        //    if (_health.IsAlive == true)
        //    {
        //        input = GetFixedInput();
        //    }

        //    if (input.Aim == true)
        //    {
        //        input.Aim &= CanAim(kccFixedData);
        //    }

        //    if (input.Aim == true)
        //    {
        //        if (_weapons.CurrentWeapon != null && _weapons.CurrentWeapon.HitType == EHitType.Sniper)
        //        {
        //            input.LookRotationDelta *= 0.3f;
        //        }
        //    }

        //    kcc.SetAim(input.Aim);

        //    if (WasActivated(EGameplayInputAction.Jump, input) == true && _character.AnimationController.CanJump() == true)
        //    {
        //        kcc.Jump(Vector3.up * _jumpPower);
        //    }

        //    SetLookRotation(kccFixedData, input.LookRotationDelta, _weapons.GetRecoil(), out Vector2 newRecoil);
        //    _weapons.SetRecoil(newRecoil);

        //    kcc.SetInputDirection(input.MoveDirection.IsZero() == true ? Vector3.zero : kcc.FixedData.TransformRotation * input.MoveDirection.X0Y());

        //    if (WasActivated(EGameplayInputAction.ToggleSide, input) == true)
        //    {
        //        LeftSide = !LeftSide;
        //    }

        //    if (WasActivated(EGameplayInputAction.ToggleSpeed, input) == true)
        //    {
        //        if (kcc.HasModifier(_fastMovementProcessor) == true)
        //        {
        //            kcc.RemoveModifier(_fastMovementProcessor);
        //        }
        //        else
        //        {
        //            kcc.AddModifier(_fastMovementProcessor);
        //        }
        //    }

        //    if (input.Weapon > 0 &&
        //        _character.AnimationController.CanSwitchWeapons(true) == true
        //        && _weapons.SwitchWeapon(input.Weapon - 1) == true)
        //    {
        //        _character.AnimationController.SwitchWeapons();
        //    }
        //    else if (input.Weapon <= 0 &&
        //        _weapons.PendingWeaponSlot != _weapons.CurrentWeaponSlot
        //        && _character.AnimationController.CanSwitchWeapons(false) == true)
        //    {
        //        _character.AnimationController.SwitchWeapons();
        //    }

        //    if (WasActivated(EGameplayInputAction.ToggleJetpack, input) == true)
        //    {
        //        if (_jetpack.IsActive == true)
        //        {
        //            _jetpack.Deactivate();
        //        }
        //        else if (_character.AnimationController.CanSwitchWeapons(true) == true)
        //        {
        //            _jetpack.Activate();
        //        }
        //    }

        //    if (_jetpack.IsActive == true)
        //    {
        //        _jetpack.FullThrust = input.Thrust;
        //    }

        //    SetFixedInput(input, false);
        //}

        //protected override void ProcessRenderInput()
        //{
        //    if (Object.HasInputAuthority == false)
        //        return;

        //    KCC kcc = _character.CharacterController;
        //    KCCData kccFixedData = kcc.FixedData;

        //    GameplayInput input = default;

        //    if (_health.IsAlive == true)
        //    {
        //        input = _agentInput.RenderInput;

        //        var cachedInput = _agentInput.CachedInput;

        //        input.LookRotationDelta = cachedInput.LookRotationDelta;
        //        input.Aim = cachedInput.Aim;
        //        input.Thrust = cachedInput.Thrust;
        //    }

        //    if (input.Aim == true)
        //    {
        //        input.Aim &= CanAim(kccFixedData);
        //    }

        //    if (input.Aim == true)
        //    {
        //        if (_weapons.CurrentWeapon != null && _weapons.CurrentWeapon.HitType == EHitType.Sniper)
        //        {
        //            input.LookRotationDelta *= 0.3f;
        //        }
        //    }

        //    SetLookRotation(kccFixedData, input.LookRotationDelta, _weapons.GetRecoil(), out Vector2 newRecoil);

        //    kcc.SetInputDirection(input.MoveDirection.IsZero() == true ? Vector3.zero : kcc.RenderData.TransformRotation * input.MoveDirection.X0Y());

        //    kcc.SetAim(input.Aim);

        //    if (WasActivated(EGameplayInputAction.Jump, input) == true && _character.AnimationController.CanJump() == true)
        //    {
        //        kcc.Jump(Vector3.up * _jumpPower);
        //    }
        //}


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


    }

}
