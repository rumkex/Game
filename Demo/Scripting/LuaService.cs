using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Calcifer.Engine.Components;
using Calcifer.Engine.Graphics.Animation;
using Calcifer.Engine.Physics;
using Calcifer.Engine.Scenery;
using Calcifer.Engine.Scripting;
using Calcifer.Utilities;
using Calcifer.Utilities.Logging;
using ComponentKit;
using ComponentKit.Model;
using System;
using Jitter.LinearMath;
using LuaInterface.Exceptions;
using OpenTK;
using OpenTK.Input;
using LuaInterface;

namespace Demo.Scripting
{
    public class LuaService: IService
    {
        private Lua lua;
		private Dictionary<LuaComponent, LuaFunction> cache; 
        private LuaComponent currentScript;
        private Random rand;
        private int lights;


        public LuaService()
        {
            lua = new Lua();
			cache = new Dictionary<LuaComponent, LuaFunction>();
            rand = new Random();
            InitializeCore();
            InitializeKeyboard();
            InitializeProperties();
            InitializeNavigation();
            InitializeNodes();
            InitializePhysics();
            InitializeAnimation();
            InitializeHealth();
            InitializeSound();
            InitializeText();
        }

        public void Synchronize(IEnumerable<IComponent> components)
        {
            foreach (var c in components.OfType<LuaComponent>())
                c.Service = c.IsOutOfSync ? null : this;
        }

        private bool halt;
        public void ExecuteScript(LuaComponent script)
        {
            if (halt)
                return;
	        if (!cache.ContainsKey(script))
		        cache[script] = lua.LoadString(script.Source, script.Record.Name.Replace(".", "")+".update");
			currentScript = script;
            lua["this"] = currentScript.Record.Name;
	        var watch = new Stopwatch();
            try
            {
	            var name = currentScript.Record.Name;
				watch.Start();
	            cache[script].Call();
	            watch.Stop();
	            var elapsed = watch.Elapsed.TotalMilliseconds;
				if (elapsed > 15)
				{
					Log.WriteLine(LogLevel.Warning, "{0} script: took {1}", name, elapsed);
					//blacklist.Add(script);
				}
            }
            catch (LuaException ex)
            {
                Log.WriteLine(LogLevel.Error, "{0} at {1}", ex.Message, ex.Source);
                halt = true;
            }
        }

        private void InitializeCore()
        {
			//lua.RegisterFunction("log", this, new Action<string>(s => Log.WriteLine(LogLevel.Info, s)).Method);
			lua.RegisterFunction("log", this, new Action<string>(s => { }).Method);
            lua.RegisterFunction("lighting", this, new Action<int>(i => lights = i).Method);
            lua.RegisterFunction("take_camera", this, new Action(() => Get<TransformComponent>("viewer").Translation = currentScript.Record.GetComponent<TransformComponent>().Translation).Method);
            lua.RegisterFunction("create_valid_object_name", this, new Func<string, string>((name) => name + "." + rand.Next(0, 32768).ToString(CultureInfo.InvariantCulture)).Method);
            lua.RegisterFunction("location", this, new Func<string>(() => "There ain't no way I'm tellin' ya that, punk").Method);
            lua.RegisterFunction("get_name", this, new Func<string>(() => currentScript.Record.Name).Method);
            lua.RegisterFunction("append_object", this, new Action<string, string, string>(AddObject).Method);
            lua.RegisterFunction("remove_object", this, new Action<string>(name =>
                                                                               {
                                                                                   var r = currentScript.Record.Registry;
                                                                                   r.Drop(Entity.Find(name));
                                                                                   r.Synchronize();
                                                                               }).Method);
            lua.RegisterFunction("has_waited", this, new Func<string, bool>(name => !Get<LuaComponent>(name).IsWaiting).Method);
            lua.RegisterFunction("wait", this, new Action<string, int>((name, count) => Get<LuaComponent>(name).Wait(count/30f)).Method);
        }
        
        private void InitializeKeyboard()
        {
            lua.RegisterFunction("key_f1", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.F1)).Method);
            lua.RegisterFunction("key_f2", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.F2)).Method);
            lua.RegisterFunction("key_f3", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.F3)).Method);
            lua.RegisterFunction("key_f4", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.F4)).Method);
            lua.RegisterFunction("key_space", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.Space)).Method);
            lua.RegisterFunction("key_right", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.Right)).Method);
            lua.RegisterFunction("key_left", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.Left)).Method);
            lua.RegisterFunction("key_up", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.Up)).Method);
            lua.RegisterFunction("key_down", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.Down)).Method);
            lua.RegisterFunction("key_shift", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.ShiftLeft) || Keyboard.GetState().IsKeyDown(Key.ShiftRight)).Method);
            lua.RegisterFunction("key_control", this, new Func<bool>(() => Keyboard.GetState().IsKeyDown(Key.ControlLeft) || Keyboard.GetState().IsKeyDown(Key.ControlRight)).Method);
        }
        private void InitializeProperties()
		{
			lua.RegisterFunction("can_walk", this, new Func<string, bool>(name => Get<MotionComponent>(name).IsOnGround).Method);

            lua.RegisterFunction("set_can_push", this, new Action<string, bool>((name, value) => Get<PlayerStateComponent>(name).CanPush = value).Method);
            lua.RegisterFunction("get_can_push", this, new Func<string, bool>(name => Get<PlayerStateComponent>(name).CanPush).Method);
            lua.RegisterFunction("can_climb", this, new Func<string, bool>(name => Get<PlayerStateComponent>(name).CanClimb).Method);
            lua.RegisterFunction("can_get_over", this, new Func<string, bool>(name => Get<PlayerStateComponent>(name).CanGetOver).Method);
            lua.RegisterFunction("can_hit_wall", this, new Func<string, bool>(name => Get<PlayerStateComponent>(name).CanHitWall).Method);
        }
        private void InitializeNavigation()
        {
            lua.RegisterFunction("get_pos_x", this, new Func<string, float>(name => Get<TransformComponent>(name).Translation.X).Method);
            lua.RegisterFunction("get_pos_y", this, new Func<string, float>(name => Get<TransformComponent>(name).Translation.Y).Method);
            lua.RegisterFunction("get_pos_z", this, new Func<string, float>(name => Get<TransformComponent>(name).Translation.Z).Method);
            lua.RegisterFunction("get_rot_x", this, new Func<string, float>(name => Get<TransformComponent>(name).Rotation.ToEuler().X * 180f / 3.14159274f).Method);
            lua.RegisterFunction("get_rot_y", this, new Func<string, float>(name => Get<TransformComponent>(name).Rotation.ToEuler().Y * 180f / 3.14159274f).Method);
            lua.RegisterFunction("get_rot_z", this, new Func<string, float>(name => Get<TransformComponent>(name).Rotation.ToEuler().Z * 180f / 3.14159274f).Method);
            lua.RegisterFunction("angle", this, new Func<string, string, double>(GetAngle).Method);
            lua.RegisterFunction("distance", this, new Func<string, string, double>((name1, name2) => Distance(name1, name2)).Method);
            lua.RegisterFunction("move_step_local", this, new Action<string, float, float, float>((name, x, y, z) =>
	                {
						var v = Vector3.Transform(-new Vector3(x, y, z) * 30f, Get<TransformComponent>(name).Rotation);
						Get<MotionComponent>(name).SetTargetVelocity(v);
	                }).Method);
            lua.RegisterFunction("move_step", this, new Action<string, float, float, float>((name, x, y, z) => Get<MotionComponent>(name).SetTargetVelocity(30f * new Vector3(x, y, z))).Method);
            lua.RegisterFunction("rotate_step", this, new Action<string, float, float, float>((name, x, y, z) => Get<MotionComponent>(name).SetAngularVelocity(30f * MathHelper.Pi / 180f * new Vector3(x, y, z))).Method);
			lua.RegisterFunction("jump", this, new Action<string>(name => Get<MotionComponent>(name).Jump()).Method);
        }

        private void InitializeNodes()
        {
            lua.RegisterFunction("get_node", this,  new Func<string, int>(name => Get<WaypointComponent>(name).CurrentNode).Method);
            lua.RegisterFunction("set_node", this,  new Action<string, int>((name, id) => Get<WaypointComponent>(name).CurrentNode = id).Method);
            lua.RegisterFunction("move_to_node", this,  new Action(() => Get<WaypointMovableComponent>(currentScript.Record.Name).Activate()).Method);
            lua.RegisterFunction("is_at_node", this,  new Func<string, bool>(name => Distance(name, Get<WaypointComponent>(name).Nodes[Get<WaypointComponent>(name).CurrentNode].Name) < 0.5f).Method);
            lua.RegisterFunction("distance_to_node", this,
								 new Func<string, int, float>((name, id) => Distance(name, Get<WaypointComponent>(name).Nodes[id].Name)).Method);
			lua.RegisterFunction("angle_to_node", this,
								  new Func<string, int, double>((name, id) => GetAngle(name, Get<WaypointComponent>(name).Nodes[id].Name)).Method);
        }

        private void InitializePhysics()
        {
            lua.RegisterFunction("set_pos", this, new Action<string, float, float, float>((name, x, y, z) => Get<TransformComponent>(name).Translation = new Vector3(x, y, z)).Method);
            lua.RegisterFunction("collision_between", this, new Func<string, string, bool>(
                (sensor, entity) => Get<PhysicsComponent>(sensor).CollidesWith(Get<PhysicsComponent>(entity).Body)
                ).Method);
            lua.RegisterFunction("set_gravity", this,  new Action<string, float>((name, value) => { }).Method);
            lua.RegisterFunction("set_restitution", this, new Action<string, float>((name, value) => { }).Method);
            lua.RegisterFunction("set_speed", this, new Action<string, float, float, float>((name, x, y, z) => Get<PhysicsComponent>(name).Body.LinearVelocity = 30f * new JVector(x, y, z)).Method);
            lua.RegisterFunction("get_floor_material", this, new Func<string, string>(name => Get<MotionComponent>(name).GetFloorMaterial()).Method);
        }

        private void InitializeAnimation()
        {
            lua.RegisterFunction("set_anim", this,  new Action<string, string>((name, anim) => GetAnimationController(name).Start(anim, true)).Method);
            lua.RegisterFunction("get_anim", this,  new Func<string, string>(name => Get<AnimationComponent>(name).Name).Method);
            lua.RegisterFunction("get_frame", this,  new Func<string, float>(name =>
                                                          {
                                                              var animationController = GetAnimationController(name);
                                                              return (int) (2.0f * animationController.Time*animationController.Speed);
                                                          }).Method);
            lua.RegisterFunction("get_frame_ratio", this, new Func<string, float>(name => GetAnimationController(name).Speed).Method);
            lua.RegisterFunction("is_anim_finished", this,  new Func<string, bool>(name =>
	                                                                                   {
																						   var animationController = GetAnimationController(name);
		                                                                                   return animationController.Length - animationController.Time <
			                                                                                   1.0f/animationController.Speed;
	                                                                                   }).Method);
        }

        private Dictionary<string, bool> wounded = new Dictionary<string, bool>(); 

        private void InitializeHealth()
        {
            lua.RegisterFunction("get_health", this, new Func<string, int>(name => GetHealthComponent(name).Health).Method);
            lua.RegisterFunction("set_health", this,  new Action<string, int>((name, value) => GetHealthComponent(name).Health = value).Method);

            lua.RegisterFunction("get_wounded", this,  new Func<string, bool>(name =>
                                                                                  {
                                                                                      if (!wounded.ContainsKey(name)) wounded.Add(name, false);
                                                                                      return wounded[name];
                                                                                  }).Method);
            lua.RegisterFunction("set_wounded", this,  new Action<string, bool>(SetWounded).Method);
        }

        public void SetWounded(string name, bool value)
        {
            if (!wounded.ContainsKey(name)) wounded.Add(name, false);
            wounded[name] = value;
        }

        private void InitializeSound()
        {
            lua.RegisterFunction("get_sound", this,  new Func<string, string>(name => name).Method);
            lua.RegisterFunction("play_sound", this,  new Action<string, string>((owner, sound) => PlaySound(owner, sound, false, 1f)).Method);
            lua.RegisterFunction("play_sound_loop", this,  new Action<string, string>((owner, sound) => PlaySound(owner, sound, true, 1f)).Method);
            lua.RegisterFunction("play_sound_random_pitch", this,  new Action<string, string>((owner, sound) => PlaySound(owner, sound, false, 0.95f + 0.1f * (float)rand.NextDouble())).Method);
        }

        private void InitializeText()
        {
            lua.RegisterFunction("get_choice", this, new Func<string>(() => "choice").Method);
            lua.RegisterFunction("i_set_choices", this, new Action<LuaTable>(t => {foreach (var o in t.Values) Console.WriteLine(o);}).Method);
			lua.DoString("function set_choices(...) i_set_choices({...}) end");
            lua.RegisterFunction("i_set_messages", this, new Action<LuaTable>(t => {foreach (var o in t.Values) Console.WriteLine(o);}).Method);
	        lua.DoString("function set_messages(...) i_set_messages({...}) end");
        }

        private double GetAngle(string name1, string name2)
        {
            var t1 = Get<TransformComponent>(name1);
            var t2 = Get<TransformComponent>(name2);
            Vector3 direction = Vector3.Transform(Vector3.UnitX, t1.Rotation);
            Vector3 distance = Vector3.Normalize(t2.Translation - t1.Translation);
            var cos = Vector3.Dot(direction, distance);
            var sin = Vector3.Cross(direction, distance).Z;
            return Math.Atan2(cos, sin);
        }

        private float Distance(string name1, string name2)
        {
            var transform = Get<TransformComponent>(name1).Transform;
            var transform2 = Get<TransformComponent>(name2).Transform;
            return (transform.Translation - transform2.Translation).Length;
        }

        private BlendAnimationController GetAnimationController(string name)
        {
            var blendAnimationController = Get<AnimationComponent>(name) as BlendAnimationController;
            if (blendAnimationController != null)
            {
                return blendAnimationController;
            }
            throw new LuaException("this AnimationComponent isn't BlendAnimationController");
        }

        private T Get<T>(string name) where T : Component
        {
	        var e = Entity.Find(name);
            if (e == null) return null;
		    var t = e.GetComponent(default(T), true);
			return t;
        }

        private HealthComponent GetHealthComponent(string name)
        {
            var healthComponent = Entity.Find(name).GetComponent<HealthComponent>();
            if (healthComponent != null)
                return healthComponent;
            throw new LuaException("Immortal object! HealthComponent not present in " + name);
        }

        private void PlaySound(string owner, string sound, bool looped, float pitch)
        {
            //Log.WriteLine(LogLevel.Warning, "Sound playing not yet implemented.");
        }

        private void AddObject(string map, string nameInMap, string name)
        {
            var pos = nameInMap.LastIndexOf('.');
            var entityClass = (pos < 0) ? nameInMap : nameInMap.Substring(0, pos);
            var e = EntityFactory.Create(name, entityClass);
            EntityRegistry.Current.Synchronize();
        }
    }
}