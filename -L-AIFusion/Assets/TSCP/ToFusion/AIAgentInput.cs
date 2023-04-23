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
        protected override void OnInput(NetworkRunner runner, NetworkInput networkInput)
        {
            base.OnInput(runner, networkInput);
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

            if (_agent.IsLocal == false || Context.HasInput == false)
                return;
            if ((Context.Input.IsCursorVisible == true && Context.Settings.SimulateMobileInput == false) || Context.GameplayMode.State != GameplayMode.EState.Active)
                return;

            _renderInput = _AIInput;

            // Process cached input for next OnInput() call, represents accumulated inputs for all render frames since last fixed update.

            float deltaTime = Time.deltaTime;

            // Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
            // If the player hits W key in last frame before fixed update, the KCC will move in render update by (velocity * 0.003333f).
            // Treating this input the same way for next fixed update results in KCC moving by (velocity * 0.03333f) - 10x more.
            // Following accumulation proportionally scales move direction so it reflects frames in which input was active.
            // This way the next fixed update will correspond more accurately to what happened in render frames.

            _cachedMoveDirection += _AIInput.MoveDirection * deltaTime;
            _cachedMoveDirectionSize += deltaTime;

            _cachedInput.Actions = new NetworkButtons(_cachedInput.Actions.Bits | _renderInput.Actions.Bits);
            _cachedInput.MoveDirection = _cachedMoveDirection / _cachedMoveDirectionSize;
            _cachedInput.LookRotationDelta += _renderInput.LookRotationDelta;

            if (_renderInput.Weapon != default)
            {
                _cachedInput.Weapon = _renderInput.Weapon;
            }
        }
    }
}
