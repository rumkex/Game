﻿using System;
using Calcifer.Engine.Physics;
using Calcifer.Utilities.Logging;
using ComponentKit;
using ComponentKit.Model;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;

namespace Demo.Components
{
    // Defines physics behaviour as following:
    // * Body collides only with static geometry
    // * Component detects when body is passing through certain non-static objects
    // * Component raises Hit event.
    public class ProjectileComponent: DependencyComponent
    {
        [RequireComponent] private PhysicsComponent physics;

        public event EventHandler<SensorEventArgs> EntityHit;
		
		public ProjectileComponent()
		{
		    collideHandler = OnCollide;
			syncHandler = Synchronized;
		}

		private Action<RigidBody, RigidBody> collideHandler;
		private EventHandler<ComponentStateEventArgs> syncHandler;

		protected override void OnAdded(ComponentStateEventArgs registrationArgs)
		{
			base.OnAdded(registrationArgs);
			if (physics.Body.Shape is Multishape) throw new NotSupportedException("Multishapes not supported!");
            physics.Body.BroadphaseTag |= (int)BodyTags.Projectile;
            physics.Body.SetMassProperties(JMatrix.Identity, 0.1f, false);
			physics.Synchronized += syncHandler;
		}

		protected override void OnRemoved(ComponentStateEventArgs registrationArgs)
        {
            physics.Body.BroadphaseTag &= ~(int)BodyTags.Projectile;
            physics.Synchronized -= syncHandler;
		    physics.World.Events.BodiesBeginCollide -= collideHandler;
			base.OnRemoved(registrationArgs);
		}

		private void Synchronized(object sender, ComponentStateEventArgs componentStateEventArgs)
		{
            if (IsOutOfSync) return;
            physics.World.Events.BodiesBeginCollide += collideHandler;
		}

        private void OnCollide(RigidBody first, RigidBody second)
        {
            RigidBody other;
            if (first == physics.Body)
                other = second;
            else if (second == physics.Body)
                other = first;
            else return;
            var otherEntity = other.Tag.ToString();
            var dir = JVector.Normalize(other.Position - physics.Body.Position);
            var vel = JVector.Normalize(physics.Body.LinearVelocity);
            if (!(JVector.Dot(vel, dir) > 0f)) return;
            var entity = Entity.Find(otherEntity);
            if (entity == null) return;
            //Log.WriteLine(LogLevel.Debug, "Hit detected on {0}", entity.Name);
            OnHit(entity);
            
        }

        private void OnHit(IEntityRecord entity)
        {
            if (EntityHit != null) EntityHit(this, new SensorEventArgs(entity));
        }
    }
}
