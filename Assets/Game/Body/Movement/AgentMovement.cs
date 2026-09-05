using System;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentMovement : Movement
    {
        private NavMeshAgent agent;

        protected override void Awake()
        {
            base.Awake();

            agent = GetComponent<NavMeshAgent>();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        protected override bool Advance(Vector3 wish, float dt)
        {
            if (agent.nextPosition != transform.position && !agent.Warp(transform.position))
                throw new InvalidOperationException($"Entity {name} stands off the navmesh at {transform.position}");

            agent.Move(wish * dt);
            transform.position = agent.nextPosition;
            return true;
        }

        protected override void Relocate(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
