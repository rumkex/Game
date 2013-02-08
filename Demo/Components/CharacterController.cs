using System;
using Calcifer.Engine.Physics;
using Calcifer.Utilities.Logging;
using Jitter;
using Jitter.Dynamics;
using Jitter.Dynamics.Constraints;
using Jitter.LinearMath;

namespace Demo.Components
{
    public enum MotionState
    {
        Grounded,
        Jumping,
        Falling,
        Climbing,
        ClimbOver
    }

    public class StateChangeEventArgs: EventArgs
    {
        public StateChangeEventArgs(MotionState oldState, MotionState state)
        {
            Previous = oldState;
            Current = state;
        }

        public MotionState Previous { get; set; }
        public MotionState Current { get; set; }
    }

    public class CharacterController : Constraint
    {
        public MotionState State { get; private set; }

        private JVector normal = JVector.Backward;
        private JVector targetVelocity;
        private bool tryJump, tryClimb;
        private World world;

        public event EventHandler<StateChangeEventArgs> StateChanged; 

        public CharacterController(World world, RigidBody body) : base(body, null)
        {
            this.world = world;
			JMatrix orient = body.Orientation;
            JVector down = JVector.Forward, ray;
			JVector.TransposedTransform(ref down, ref orient, out down);
            body.Shape.SupportMapping(ref down, out ray);
			FeetPosition = ray * down;
            JumpVelocity = 5f;
            FallVelocity = 5f;
            ClimbVelocity = 1f;
        }

        public float JumpVelocity { get; set; }
        public float FallVelocity { get; set; }
        public float ClimbVelocity { get; set; }
        public RigidBody BodyWalkingOn { get; private set; }
        public float FeetPosition { get; private set; }

        public override void PrepareForIteration(float timestep)
        {
            RigidBody body;
            float depth;

            bool collidesWithLadder = world.CollisionSystem.Raycast(Body1.Position + JVector.Forward * (FeetPosition - 0.1f),
                new JVector(0, 1, 0), 
                (b, n, f) => b != Body1 && (b.BroadphaseTag & (int)BodyTags.Ghost) == 0,
                out body, out normal, out depth);
            bool collidesWithGround = world.CollisionSystem.Raycast(Body1.Position + JVector.Forward * (FeetPosition - 0.1f),
                JVector.Forward, 
                (b, n, f) => b != Body1 && (b.BroadphaseTag & (int)BodyTags.Ghost) == 0,
                out body, out normal, out depth);
            BodyWalkingOn = ((!collidesWithGround || depth > 0.2f) ? null : body);
            var oldState = State;
            switch (oldState)
            {
                case MotionState.Grounded:
                    if (BodyWalkingOn != null)
                    {
                        if (tryClimb)
                        {
                            State = MotionState.Climbing;
                            tryClimb = false;
                        }
                        else if (Body1.LinearVelocity.Z < JumpVelocity && tryJump) State = MotionState.Jumping;
                    }
                    else if (-Body1.LinearVelocity.Z < FallVelocity) State = MotionState.Falling;
                    break;
                case MotionState.Jumping:
                    if (-Body1.LinearVelocity.Z < FallVelocity) State = MotionState.Falling;
                    else if (depth < 0.1f && Body1.LinearVelocity.Z < 0.0f) State = MotionState.Grounded;
                    break;
                case MotionState.Falling:
                    if (BodyWalkingOn != null && depth < 0.1f && Body1.LinearVelocity.Z < 0.0f) State = MotionState.Grounded;
                    break;
                case MotionState.Climbing:
                    if (BodyWalkingOn == null && !collidesWithLadder) 
                        State = MotionState.Grounded;
                    break;
            }
            if (State != oldState)
            {
                if (StateChanged != null) StateChanged(this, new StateChangeEventArgs(oldState, State));
                Log.WriteLine(LogLevel.Debug, "switched from {0} to {1}", oldState, State);
            }
        }

        public override void Iterate()
        {
            // Controlled movement happens in every State
            var deltaVelocity = targetVelocity - Body1.LinearVelocity;
            if (BodyWalkingOn != null) deltaVelocity += BodyWalkingOn.LinearVelocity;
            deltaVelocity -= JVector.Dot(deltaVelocity, normal) * normal;
            // However while in the air, control is greatly reduced
            deltaVelocity *= (State == MotionState.Grounded || State == MotionState.Climbing) ? 0.2f: 0.01f;
            if (deltaVelocity.LengthSquared() > 0.000001f)
            {
                Body1.ApplyImpulse(deltaVelocity * Body1.Mass);
            }
            switch (State)
            {
                case MotionState.Grounded:
                    // If player just stands on the ground,
                    // this reduces overall jumpiness
                    // and glues player to the ground
                    if (BodyWalkingOn != null)
                    {
                        var nvel = (Body1.LinearVelocity - BodyWalkingOn.LinearVelocity) * normal;
                        Body1.LinearVelocity -= 0.7f*nvel*normal;
                    }
                    break;
                case MotionState.Falling:
                    // First let the gravity do its job
                    if (-Body1.LinearVelocity.Z < FallVelocity) break;
                    // If it's moving too fast, constrain the falling velocity
                    var dv = 0.5f * (FallVelocity + Body1.LinearVelocity.Z);
                    Body1.LinearVelocity += dv*JVector.Forward;
                    break;
                case MotionState.Jumping:
                    // There are multiple iterations per step, so apply jump impulse only once
                    if (!tryJump) break;
                    Body1.ApplyImpulse(JumpVelocity*JVector.Backward*Body1.Mass);
                    Log.WriteLine(LogLevel.Debug, "JUMP!");
	                tryJump = false;
                    break;
                case MotionState.Climbing:
                    var zVel = Body1.LinearVelocity.Z;
                    var climbFactor = targetVelocity.IsZero() ? 0 : JVector.Dot(targetVelocity, new JVector(0, 1, 0)) / targetVelocity.Length();
                    var delta = 0.8f * (climbFactor * ClimbVelocity - zVel);
                    Console.WriteLine("{0}, {1}", climbFactor, targetVelocity);
                    Body1.ApplyImpulse(delta * JVector.Backward * Body1.Mass);
                    break;
            }
        }

        public void SetTargetVelocity(JVector vel)
        {
            targetVelocity = vel;
        }

        public void Jump()
        {
            tryJump = true;
        }

        public void ClimbUp()
        {
            tryClimb = true;
        }

        public void ClimbOver()
        {

        }
    }
}