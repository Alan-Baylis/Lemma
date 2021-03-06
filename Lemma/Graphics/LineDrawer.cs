﻿using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Serialization;

namespace Lemma.Components
{
	public class LineDrawer : Component<Main>, IDrawableAlphaComponent
	{
		public struct Line
		{
			public VertexPositionColor A;
			public VertexPositionColor B;
		}

		public Property<int> DrawOrder { get; set; }

		[XmlIgnore]
		public ListProperty<Line> Lines = new ListProperty<Line>();

		private Effect effect;

		private bool changed;

		private static List<Technique> unsupportedTechniques = new List<Technique>();

		private DynamicVertexBuffer vertexBuffer;

		public LineDrawer()
		{
			this.Serialize = false;
			this.DrawOrder = new Property<int> { Value = 11 };
		}

		public void LoadContent(bool reload)
		{
			this.effect = this.main.Content.Load<Effect>("Effects\\Lines").Clone();
		}

		public override void Awake()
		{
			base.Awake();
			this.Add(new NotifyBinding(this.main.AlphaDrawablesModified, this.DrawOrder));
			this.Add(new ListNotifyBinding<Line>(delegate() { this.changed = true; }, this.Lines));
		}

		void IDrawableAlphaComponent.DrawAlpha(Microsoft.Xna.Framework.GameTime time, RenderParameters p)
		{
			if (this.Lines.Length == 0 || LineDrawer.unsupportedTechniques.Contains(p.Technique))
				return;

			if (this.vertexBuffer == null || this.vertexBuffer.IsContentLost || this.Lines.Length * 2 > this.vertexBuffer.VertexCount || this.changed)
			{
				this.changed = false;
				if (this.vertexBuffer != null)
					this.vertexBuffer.Dispose();

				this.vertexBuffer = new DynamicVertexBuffer(this.main.GraphicsDevice, VertexPositionColor.VertexDeclaration, (this.Lines.Length * 2) + 8, BufferUsage.WriteOnly);

				VertexPositionColor[] data = new VertexPositionColor[this.vertexBuffer.VertexCount];
				for (int i = 0; i < this.Lines.Count; i++)
				{
					Line line = this.Lines[i];
					data[i * 2] = line.A;
					data[i * 2 + 1] = line.B;
				}
				this.vertexBuffer.SetData<VertexPositionColor>(data, 0, this.Lines.Length * 2, SetDataOptions.Discard);
			}

			p.Camera.SetParameters(this.effect);
			this.effect.Parameters["Depth" + Model.SamplerPostfix].SetValue(p.DepthBuffer);

			// Draw lines
			try
			{
				this.effect.CurrentTechnique = this.effect.Techniques[p.TechniqueString];
			}
			catch (Exception)
			{
				LineDrawer.unsupportedTechniques.Add(p.Technique);
				return;
			}

			this.effect.CurrentTechnique.Passes[0].Apply();
			this.main.GraphicsDevice.SetVertexBuffer(this.vertexBuffer);
			this.main.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, this.Lines.Length);
			Model.DrawCallCounter++;
			Model.TriangleCounter += this.Lines.Length;
		}

		public override void delete()
		{
			this.effect.Dispose();
			if (this.vertexBuffer != null)
				this.vertexBuffer.Dispose();
			base.delete();
		}
	}
}
