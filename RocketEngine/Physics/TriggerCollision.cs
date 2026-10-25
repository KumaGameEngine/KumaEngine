using BulletSharp;
using KumaEngine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Physics
{
    public class TriggerCollision : PairCachingGhostObject
    {
        public CollisionFilterGroups Group { get; }

        public Action<CollisionObject> OnEnter { get; set; } = null!;
        public Action<CollisionObject> OnExit { get; set; } = null!;
        public Action<CollisionObject> OnTick { get; set; } = null!;

        HashSet<CollisionObject> previousCollsions = new();

        public TriggerCollision(
            Vector3 halfSize, 
            Vector3 position, Vector3 rotation, Vector3 scale, 
            CollisionFilterGroups group = CollisionFilterGroups.AllFilter
        ) : base() {
            CollisionShape = new BoxShape(halfSize);
            WorldTransform = GameObjectAPI.ComposeTransform(position,rotation,scale);
            CollisionFlags |= CollisionFlags.NoContactResponse;

            Group = group;
        }

        public void HandleCollisions()
        {
            HashSet<CollisionObject> currentCollsions = new();

            for (int i = 0; i < NumOverlappingObjects; i++)
                currentCollsions.Add(GetOverlappingObject(i));

            if (OnEnter != null || OnTick != null)
            {
                foreach (var obj in currentCollsions)
                    if (!previousCollsions.Contains(obj) && OnEnter != null) 
                        OnEnter(obj);
                    else if (OnTick != null) 
                        OnTick(obj);
            }

            if (OnExit != null)
            {
                foreach (var obj in previousCollsions)
                    if (!currentCollsions.Contains(obj)) OnExit(obj);
            }

            previousCollsions = currentCollsions;
        }
    }
}
