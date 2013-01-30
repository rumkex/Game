using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Calcifer.Engine.Graphics;
using Calcifer.Engine.Scenegraph;
using Calcifer.UI;

namespace Demo
{
    class UIPass: RenderPass
    {
        private Canvas canvas;

        public UIPass(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public override void BeginRender(ICamera camera)
        {
            canvas.Render();
        }

        public override void Visit(SceneNode node)
        {}
    }
}
