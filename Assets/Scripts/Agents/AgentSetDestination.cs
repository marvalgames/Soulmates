using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using ProjectDawn.Navigation;

namespace Sandbox.Agents
{
    
    public class AgentSetDestination : MonoBehaviour
    {
    }

// ECS component
    public struct SetDestination : IComponentData
    {
    }

// Bakes mono component into ecs component
    class AgentSetDestinationBaker : Baker<AgentSetDestination>
    {
        public override void Bake(AgentSetDestination authoring)
        {
        }
    }
}