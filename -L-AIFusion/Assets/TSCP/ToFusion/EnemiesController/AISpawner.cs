using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CoverShooter;

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
                Debug.LogError("Spawn Enemies");

                countSpawned++;
                if (countSpawned == 1)
                    SpawnEnemies(enemyTemplate);
                else
                    SpawnEnemies(PTemplate);

                _spawnDelay = TickTimer.CreateFromSeconds(Runner, 3f);
            }
        }

        public AIAgent SpawnAI(AIPlayer aiPlayer, in Vector3 position)
        {
            var enemy = Runner.Spawn(enemyTemplate, position, Quaternion.identity, 
                                                PlayerRef.None, onBeforeSpawned: _OnBeforeSpawned);

            enemy.transform.name = "Enemy  " + UnityEngine.Random.Range(0, 10);
            var aiAgent = enemy.GetComponent<AIAgent>();

            var statistics = aiPlayer.Statistics;
            statistics.IsAlive = true;
            statistics.RespawnTimer = default;

            _currentGameMode.TryAdd(aiPlayer, aiAgent);
            statistics.AgentIndex = aiAgent.AgentIndex;

            aiPlayer.UpdateStatistics(statistics);
            aiPlayer.SetActiveAgent(enemy.GetComponent<AIAgent>());

            var fighter = aiAgent.GetComponent<FighterBrain>();
            fighter.Start.Mode = AIStartMode.investigate;
            fighter.Start.Position = _currentGameMode.FindAgentNearestPoint(fighter.transform.position, aiAgent).transform.position;
            fighter.StartingLocation = _currentGameMode.FindAgentNearestPoint(fighter.transform.position, aiAgent).transform.position;


            AllEnemies.Add(enemy);

            return aiAgent;
        }
        private void SpawnEnemies(NetworkBehaviour template)
        {
            // var spawnPoint = _currentGameMode.GetRandomSpawnPoint(1f);
            var spawnPoint = _currentGameMode.GetRandomSpawnPoint(1f);

            var deltaPosition = UnityEngine.Random.insideUnitSphere * 5f;
            deltaPosition.y = 0;

            var spawnPosition = spawnPoint != null ? spawnPoint.position + deltaPosition : Vector3.zero;
            var spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            var rotation = Quaternion.identity;

            enemyTemplate.transform.localRotation = Quaternion.identity;
            PTemplate.transform.localRotation = Quaternion.identity;

            var enemy = Runner.Spawn(template, spawnPoint.position, rotation, PlayerRef.None, onBeforeSpawned: _OnBeforeSpawned);

            //enemy.AddBehaviour<AIMovement>();
            //// enemy.AddBehaviour<AISearch>();
            //enemy.AddBehaviour<AISight>();
            //// enemy.AddBehaviour<AIAim>();
            //// enemy.AddBehaviour<AIAlerts>();
            //// enemy.AddBehaviour<AIInvestigation>();
            //enemy.AddBehaviour<FighterBrain>();

            enemy.transform.name = "Enemy  " + UnityEngine.Random.Range(0, 10);
            var aiPlayer = Context.NetworkGame.SpawnAIPlayer();
            var aiAgent = enemy.GetComponent<AIAgent>();

            var statistics = aiPlayer.Statistics;
            statistics.IsAlive = true;
            statistics.RespawnTimer = default;

            _currentGameMode.TryAdd(aiPlayer, aiAgent);
            statistics.AgentIndex = aiAgent.AgentIndex;

            var fighter = aiAgent.GetComponent<FighterBrain>();

            fighter.Start.Mode = AIStartMode.investigate;
            fighter.Start.Position = _currentGameMode.FindAgentNearestPoint(fighter.transform.position, aiAgent).transform.position;
            fighter.StartingLocation = _currentGameMode.FindAgentNearestPoint(fighter.transform.position, aiAgent).transform.position;

            aiPlayer.UpdateStatistics(statistics);
            aiPlayer.SetActiveAgent(enemy.GetComponent<AIAgent>());

            
            AllEnemies.Add(enemy);
        }

        private void _OnBeforeSpawned(NetworkRunner runner, NetworkObject obj)
        {

        }
    }
}
