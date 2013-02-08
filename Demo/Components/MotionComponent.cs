using System;
using System.Linq;
using Calcifer.Engine;
using Calcifer.Engine.Components;
using Calcifer.Engine.Graphics.Animation;
using Calcifer.Engine.Physics;
using Calcifer.Utilities;
using ComponentKit.Model;
using Jitter.LinearMath;
using OpenTK;

namespace Demo.Components
{
    public class MotionComponent : DependencyComponent, IUpdateable
    {
        [RequireComponent(AllowDerivedTypes = true)] private AnimationComponent anim;
        [RequireComponent] private PhysicsComponent phys;
        private TerrainComponent terrain;
        private PhysicsComponent terrainPhys;
        private CharacterController controller;
        private double jumpCooldown;

        public event EventHandler<StateChangeEventArgs> StateChanged; 

        protected override void OnAdded(ComponentStateEventArgs e)
        {
			base.OnAdded(e);
			phys.Body.Material.KineticFriction = 0f;
	        phys.Body.Material.StaticFriction = 0f;
            phys.Body.Material.Restitution = 0f;
	        var invInertia = phys.Body.InverseInertia;
			invInertia.M11 = 0;
			invInertia.M22 = 0;
	        invInertia.M33 = 0;
			phys.Body.SetMassProperties(invInertia, 1f, true);
			phys.Synchronized += OnSynchronized;
        }

        protected override void OnRemoved(ComponentStateEventArgs e)
        {
            if (controller != null) phys.World.RemoveConstraint(controller);
            base.OnRemoved(e);
        }

	    private void OnSynchronized(object sender, ComponentStateEventArgs e)
	    {
		    if (!IsOutOfSync)
			{
				controller = new CharacterController(phys.World, phys.Body);
                controller.StateChanged += OnStateChanged;
                phys.World.AddConstraint(controller);
                var ground = Entity.FindAllWithComponent<TerrainComponent>().FirstOrDefault();
                terrain = ground.GetComponent<TerrainComponent>();
                terrainPhys = ground.GetComponent<PhysicsComponent>();
		    }
	    }

        private void OnStateChanged(object sender, StateChangeEventArgs e)
        {
            if (StateChanged != null) StateChanged(this, e);
        }

        public MotionState State
        {
            get { return controller.State; }
        }

        public TerrainType GetFloorMaterial()
	    {
		    if (State != MotionState.Grounded) return TerrainType.None;
			var floor = Entity.Find(controller.BodyWalkingOn.Tag.ToString());
			var terrainComponent = floor.GetComponent<TerrainComponent>();
		    if (terrainComponent == null) return TerrainType.None;
			var material = terrainComponent.GetMaterial(phys.Body.Position + JVector.Forward * (controller.FeetPosition - 0.1f), JVector.Forward);
		    return material;
	    }

	    public void SetTargetVelocity(Vector3 speed)
        {
	        phys.Body.IsActive = true;
            controller.SetTargetVelocity(speed.ToJVector());
        }

        public void Jump()
		{
			phys.Body.IsActive = true;
	        if (jumpCooldown > 0) return;
			jumpCooldown = 1.0;
            controller.Jump();
		}

	    public void Update(double t)
	    {
			if (jumpCooldown > 0) jumpCooldown -= t;
            if (phys.CollidesWith(terrainPhys.Body))
            {
                var start = phys.Body.Position;
                var delta = Vector3.Transform(-Vector3.UnitY, Record.GetComponent<TransformComponent>().Rotation);
                switch (terrain.GetMaterial(start, delta.ToJVector()))
                {
                    case TerrainType.Ladder:
                        controller.ClimbUp();
                        break;
                    case TerrainType.Obstacle:
                        controller.ClimbOver();
                        break;
                }
            }
		}

	    public void SetAngularVelocity(Vector3 w)
	    {
		    phys.Body.AngularVelocity = w.ToJVector();
	    }
    }
}