using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Calcifer.Engine;
using Calcifer.Engine.Content;
using Calcifer.Engine.Physics;
using Calcifer.Engine.Scenery;
using ComponentKit.Model;
using Demo.Import;
using Jitter.Collision;
using Jitter.LinearMath;

namespace Demo.Components
{
    public enum TerrainType
    {
        [Description("")]
        None = 0,
        [Description("grass_level.png")]
        Grass,
        [Description("wood_level.png")]
        Wood,
        [Description("dirt_level.png")]
        Dirt,
        [Description("metal_level.png")]
        Metal,
        [Description("snow_level.png")]
        Snow,
        [Description("water_level.png")]
        Water,
        [Description("ladder_level.png")]
        Ladder,
        [Description("obstacle_level.png")]
        Obstacle,
    }

    internal static class TerrainTypeExtensions
    {
        public static string GetDescription(this TerrainType value)
        {
            var enumType = value.GetType();
            var field = enumType.GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute),
                                                       false);
            return attributes.Length == 0
                ? value.ToString()
                : ((DescriptionAttribute)attributes[0]).Description;
        }
    }

	public class TerrainComponent: DependencyComponent, IConstructable
	{
	    private MaterialData materials;
	    private Octree octree;
		
		public TerrainType GetMaterial(JVector start, JVector delta)
		{
		    var result = GetTriangles(start, delta);
		    return result.Count == 0 ? TerrainType.None : materials.GetMaterial(result[0]);
		}

        public List<int> GetTriangles(JVector start, JVector delta)
        {
            var result = new List<int>();
            octree.GetTrianglesIntersectingRay(result, start, delta);
            return result;
        }

	    void IConstructable.Construct(IDictionary<string, string> param)
        {
            var physData = ResourceFactory.LoadAsset<PhysicsData>(param["physData"]);
            materials = ResourceFactory.LoadAsset<MaterialData>(param["physData"]);
	        octree = physData.Octree;
        }
	}
}
