using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    // The AsteroidSpawner does not execute any behaviour on the clients.
    // Therefore all of its parameters can remained local and not networked.
    // Using the SimulationBehaviour attribute, it can be ensured this script exclusively runs on the Server / Host
    [SimulationBehaviour(Stages = SimulationStages.Forward, Modes = SimulationModes.Server | SimulationModes.Host)]
    public class AISpawner : ContextBehaviour
    {
        [SerializeField] private bool _disableSpawner = false;
        [SerializeField] protected NetworkBehaviour enemyTemplate;
        [SerializeField] protected NetworkBehaviour PTemplate;
        // The TickTimer controls the time lapse between spawns.
        [Networked] private TickTimer _spawnDelay { get; set; }

        public override void Spawned()
        {
            _spawnDelay = TickTimer.CreateFromSeconds(Runner, 5f);
            base.Spawned();
        }
        public List<NetworkBehaviour> AllEnemies = new List<NetworkBehaviour>();

        [SerializeField] NetworkGame networkGame;
        private GameplayMode _currentGameMode;

        protected int countSpawned = 0;



        public void InitGameMode(GameplayMode gameMode) => this._currentGameMode = gameMode;
        public override void FixedUpdateNetwork()
        {
            if (_disableSpawner || !Object.HasStateAuthority || _currentGameMode == null)
                return;

            if (countSpawned <= 0 && _spawnDelay.Expired(Runner))
            {
                countSpawned++;
                if (countSpawned == 1)
                    SpawnEnemies(enemyTemplate);
                else
                    SpawnEnemies(PTemplate);

                _spawnDelay = TickTimer.CreateFromSeconds(Runner, 3f);
            }
        }
        private void SpawnEnemies(NetworkBehaviour template)
        {
            // var spawnPoint = _currentGameMode.GetRandomSpawnPoint(1f);
            var spawnPoint = _currentGameMode.GetAgents()[0].transform;

            var deltaPosition = UnityEngine.Random.insideUnitSphere * 5f;
            deltaPosition.y = 0;

            var spawnPosition = spawnPoint != null ? spawnPoint.position + deltaPosition : Vector3.zero;
            var spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            var rotation = Quaternion.identity;

            enemyTemplate.transform.localRotation = Quaternion.identity;
            PTemplate.transform.localRotation = Quaternion.identity;

            var enemy = Runner.Spawn(template, spawnPoint.position, rotation, Context.LocalPlayerRef, onBeforeSpawned: _OnBeforeSpawned);
            Debug.Break();
            AllEnemies.Add(enemy);
        }

        private void _OnBeforeSpawned(NetworkRunner runner, NetworkObject obj)
        {

        }
    }
}
