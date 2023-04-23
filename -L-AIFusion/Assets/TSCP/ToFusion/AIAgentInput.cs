using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class AIAgentInput : AgentInput
    {
        protected GameplayInput _AIInput;
        public ref GameplayInput GetInput() => ref _AIInput;

        public override void SetFixedInput(GameplayInput fixedInput, bool updateBaseInputs)
        {
            base.SetFixedInput(fixedInput, updateBaseInputs);
            if (updateBaseInputs)
            {
                _AIInput = fixedInput;
            }
        }

        public override void SetLastKnownInput(GameplayInput fixedInput, bool updateBaseInputs)
        {
            base.SetLastKnownInput(fixedInput, updateBaseInputs);
            if (updateBaseInputs)
            {
                _AIInput = fixedInput;
            }
        }
        protected override void OnInput(NetworkRunner runner, NetworkInput networkInput)
        {
            if (_agent.IsLocal == false || Context.HasInput == false)
            {
                _cachedInput    = default;
                _renderInput    = default;
                _AIInput        = default;
                return;
            }

            _resetCachedInput = true;

            // Now we reset all properties which should not propagate into next OnInput() call (for example LookRotationDelta - this must be applied only once and reset immediately).
            // If there's a spike, OnInput() and FixedUpdateNetwork() will be called multiple times in a row without BeforeUpdate() in between, so we don't reset move direction to preserve movement.
            // Instead, move direction and other sensitive properties are reset in next BeforeUpdate() - driven by _resetCachedInput.

            _cachedInput.LookRotationDelta = default;

            // Input consumed by OnInput() call will be read in FixedUpdateNetwork() and immediately propagated to KCC.
            // Here we should reset render properties so they are not applied twice (fixed + render update).

            _renderInput.LookRotationDelta = default;
        }

        public override void Spawned()
        {
            // Reset to default state.
            _fixedInput = default;
            _renderInput = default;
            _cachedInput = default;
            _lastKnownInput = default;
            _baseFixedInput = default;
            _baseRenderInput = default;
            _missingInputsTotal = default;
            _missingInputsInRow = default;
            _AIInput = default;
            // Wait few seconds before the connection is stable to start tracking missing inputs.
            _logMissingInputFromTick = Runner.Simulation.Tick + Runner.Config.Simulation.TickRate * 4;
        }
        public override void BeforeUpdate()
        {
            if (Object.HasInputAuthority == false)
                return;

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

            if (_agent.IsLocal == false)
                return;

            //if (_agent.IsLocal == false || Context.HasInput == false)
            //    return;
            //if ((Context.Input.IsCursorVisible == true && Context.Settings.SimulateMobileInput == false) || Context.GameplayMode.State != GameplayMode.EState.Active)
            //    return;

            _renderInput = _AIInput;

            // Process cached input for next OnInput() call, represents accumulated inputs for all render frames since last fixed update.

            float deltaTime = Time.deltaTime;

            // Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
            // If the player hits W key in last frame before fixed update, the KCC will move in render update by (velocity * 0.003333f).
            // Treating this input the same way for next fixed update results in KCC moving by (velocity * 0.03333f) - 10x more.
            // Following accumulation proportionally scales move direction so it reflects frames in which input was active.
            // This way the next fixed update will correspond more accurately to what happened in render frames.

            _cachedMoveDirection += __lastKnow.MoveDirection * deltaTime;
            _cachedMoveDirectionSize += deltaTime;

            _cachedInput.Actions = new NetworkButtons(_cachedInput.Actions.Bits | _renderInput.Actions.Bits);
            _cachedInput.MoveDirection = _cachedMoveDirection / _cachedMoveDirectionSize;
            _cachedInput.LookRotationDelta += _renderInput.LookRotationDelta;

            if (_renderInput.Weapon != default)
            {
                _cachedInput.Weapon = _renderInput.Weapon;
            }
        }

        public override void Render()
        {
            base.Render();
            _AIInput = default;
        }
        public override void BeforeTick()
        {
            if (Object.IsProxy == true || Context == null || Context.GameplayMode == null || Context.GameplayMode.State != GameplayMode.EState.Active)
            {
                _fixedInput = default;
                _renderInput = default;
                _cachedInput = default;
                _lastKnownInput = default;
                _baseFixedInput = default;
                _baseRenderInput = default;
                _AIInput = default;
                return;
            }

            // Store last known fixed input. This will be compared agaisnt new fixed input.
            _baseFixedInput = _lastKnownInput;

            // Set fixed input to last known fixed input as a fallback.
            _fixedInput = _lastKnownInput;
            _fixedInput = _AIInput;
            // The current fixed input will be used as a base to first Render after FUN.
            _baseRenderInput = _fixedInput;
        }
    }
}
