using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PartsTreeSystemExample
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			var env = new PartsTreeSystem.Environment();
			var commandManager = new PartsTreeSystem.CommandManager();

			PartsTreeSystem.NodeTreeGroup nodeTreeGroup = new PartsTreeSystem.NodeTreeGroup();
			nodeTreeGroup.Init(typeof(NodeStruct), env);

			var nodeTree = PartsTreeSystem.Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			Altseed2.Configuration configuration = new Altseed2.Configuration();
			configuration.EnabledCoreModules = Altseed2.CoreModules.Default | Altseed2.CoreModules.Tool;
			if (!Altseed2.Engine.Initialize("Example", 640, 480, configuration))
			{
				return;
			}

			Node selectedNode = null;
			Node popupedNode = null;

			while (Altseed2.Engine.DoEvents())
			{
				if (Altseed2.Engine.Tool.Begin("Command", Altseed2.ToolWindowFlags.NoCollapse))
				{
					if (Altseed2.Engine.Tool.Button("Undo"))
					{
						commandManager.Undo(env);
					}

					if (Altseed2.Engine.Tool.Button("Redo"))
					{
						commandManager.Redo(env);
					}

					if (Altseed2.Engine.Tool.Button("Save"))
					{
						var path = Altseed2.Engine.Tool.SaveDialog("nodes", System.IO.Directory.GetCurrentDirectory());
						if (!string.IsNullOrEmpty(path))
						{
							var text = nodeTreeGroup.Serialize(env);
							System.IO.File.WriteAllText(path + ".nodes", text);
						}
					}

					if (Altseed2.Engine.Tool.Button("Load"))
					{
						var path = Altseed2.Engine.Tool.OpenDialog("nodes", System.IO.Directory.GetCurrentDirectory());
						if (!string.IsNullOrEmpty(path))
						{
							var text = System.IO.File.ReadAllText(path);
							nodeTreeGroup = PartsTreeSystem.NodeTreeGroup.Deserialize(text);
							nodeTree = PartsTreeSystem.Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);
							commandManager = new PartsTreeSystem.CommandManager();
						}
					}
				}

				Altseed2.Engine.Tool.End();

				if (Altseed2.Engine.Tool.Begin("NodeTree", Altseed2.ToolWindowFlags.NoCollapse))
				{
					string menuKey = "menu";

					Action<Node> updateNode = null;

					Action<Node> showNodePopup = (node) =>
					{
						if (Altseed2.Engine.Tool.IsItemHovered(Altseed2.ToolHoveredFlags.None))
						{
							if (Altseed2.Engine.Tool.IsMouseReleased(Altseed2.ToolMouseButton.Right))
							{
								Altseed2.Engine.Tool.OpenPopup(menuKey, Altseed2.ToolPopupFlags.None);
								popupedNode = node;
							}
						}
					};

					updateNode = (node) =>
					{
						var n = node as NodeStruct;

						if (Altseed2.Engine.Tool.TreeNode(n.Name + "##" + node.InstanceID))
						{
							if (Altseed2.Engine.Tool.IsItemClicked(Altseed2.ToolMouseButton.Left))
							{
								selectedNode = node;
							}

							showNodePopup(node);

							foreach (var child in node.Children)
							{
								updateNode(child);
							}
						}
						else
						{
							showNodePopup(node);
						}
					};

					updateNode(nodeTree.Root as Node);

					if (Altseed2.Engine.Tool.BeginPopup(menuKey, Altseed2.ToolWindowFlags.None))
					{
						if (Altseed2.Engine.Tool.Button("Add Node"))
						{
							commandManager.AddNode(nodeTreeGroup, nodeTree, popupedNode.InstanceID, typeof(NodeStruct), env);
							Altseed2.Engine.Tool.CloseCurrentPopup();
						}

						if (Altseed2.Engine.Tool.Button("Remove node"))
						{
							commandManager.RemoveNode(nodeTreeGroup, nodeTree, popupedNode.InstanceID, env);
							Altseed2.Engine.Tool.CloseCurrentPopup();
						}

						Altseed2.Engine.Tool.EndPopup();
					}
				}

				Altseed2.Engine.Tool.End();

				// TODO 選択されてるノードがツリー内に存在するかチェックする

				if (Altseed2.Engine.Tool.Begin("Ispector", Altseed2.ToolWindowFlags.NoCollapse))
				{
					if (selectedNode != null)
					{
						commandManager.StartEditFields(nodeTreeGroup, nodeTree, selectedNode, env);

						Action<FieldGetterSetter> updateFields = null;

						updateFields = (FieldGetterSetter getterSetter) =>
						{

							var value = getterSetter.GetValue();
							var name = getterSetter.GetName();

							if (value is string)
							{
								var s = (string)value;

								var result = Altseed2.Engine.Tool.InputText(name, s, 200, Altseed2.ToolInputTextFlags.None);
								if (result != null)
								{
									getterSetter.SetValue(result);
									commandManager.NotifyEditFields(selectedNode);
								}
							}
							if (value is int)
							{
								var v = (int)value;

								if (Altseed2.Engine.Tool.DragInt(name, ref v, 1, -100, 100, "%d", Altseed2.ToolSliderFlags.None))
								{
									getterSetter.SetValue(v);
									commandManager.NotifyEditFields(selectedNode);
								}
							}
							else if (value is float)
							{
								var v = (float)value;

								if (Altseed2.Engine.Tool.DragFloat(name, ref v, 1, -100, 100, "%f", Altseed2.ToolSliderFlags.None))
								{
									getterSetter.SetValue(v);
									commandManager.NotifyEditFields(selectedNode);
								}
							}
							else if (value is IList)
							{
								var v = (IList)value;
								var count = v.Count;
								if (Altseed2.Engine.Tool.DragInt(name, ref count, 1, 0, 100, "%d", Altseed2.ToolSliderFlags.None))
								{
									Helper.ResizeList(v, count);

									commandManager.NotifyEditFields(selectedNode);
								}

								var listGetterSetter = new FieldGetterSetter();

								for (int i = 0; i < v.Count; i++)
								{
									listGetterSetter.Reset(v, i);
									updateFields(listGetterSetter);
								}
							}
							else
							{
								Altseed2.Engine.Tool.Text(name);
							}
						};

						var fields = selectedNode.GetType().GetFields();

						var getterSetter = new FieldGetterSetter();

						foreach (var field in fields)
						{
							getterSetter.Reset(selectedNode, field);
							updateFields(getterSetter);
						}

						commandManager.EndEditFields(selectedNode, env);
					}

					if (!Altseed2.Engine.Tool.IsAnyItemActive())
					{
						commandManager.SetFlagToBlockMergeCommands();
					}
				}

				Altseed2.Engine.Tool.End();

				Altseed2.Engine.Update();
			}

			Altseed2.Engine.Terminate();
		}

		public class NodeStruct : Node
		{
			public string Name = "Node";
			public int Value1;
			public float Value2;
			public List<int> List1 = new List<int>();
		}
	}

	public class Node : PartsTreeSystem.INode
	{
		public int InstanceID { get; set; }

		[System.NonSerialized]
		public List<Node> Children = new List<Node>();

		public void AddChild(PartsTreeSystem.INode node)
		{
			Children.Add(node as Node);
		}

		public void RemoveChild(int instanceID)
		{
			Children.RemoveAll(_ => _.InstanceID == instanceID);
		}

		public IReadOnlyCollection<PartsTreeSystem.INode> GetChildren()
		{
			return Children;
		}
	}

	class FieldGetterSetter
	{
		object parent;
		System.Reflection.FieldInfo fieldInfo;
		int? index;

		public void Reset(object o, System.Reflection.FieldInfo fieldInfo)
		{
			parent = o;
			this.fieldInfo = fieldInfo;
			index = null;
		}

		public void Reset(object o, int index)
		{
			parent = o;
			fieldInfo = null;
			this.index = index;
		}

		public string GetName()
		{
			if (fieldInfo != null)
			{
				return fieldInfo.Name;
			}
			else if (index.HasValue)
			{
				return index.Value.ToString();
			}

			return string.Empty;
		}

		public object GetValue()
		{
			if (fieldInfo != null)
			{
				return fieldInfo.GetValue(parent);
			}
			else if (index.HasValue)
			{
				return Helper.GetValueWithIndex(parent, index.Value);
			}

			return null;
		}

		public void SetValue(object value)
		{
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(parent, value);
			}
			else if (index.HasValue)
			{
				Helper.SetValueToIndex(parent, value, index.Value);
			}
		}
	}

	class Helper
	{
		public static void ResizeList(IList list, int count)
		{
			while (list.Count < count)
			{
				list.Add(CreateDefaultValue(list.GetType().GetGenericArguments()[0]));
			}

			while (list.Count > count)
			{
				list.RemoveAt(list.Count - 1);
			}
		}

		public static object GetValueWithIndex(object target, int index)
		{
			foreach (var pi in target.GetType().GetProperties())
			{
				if (pi.GetIndexParameters().Length != 1)
				{
					continue;
				}

				return pi.GetValue(target, new object[] { index });
			}
			return null;
		}

		public static bool SetValueToIndex(object target, object value, int index)
		{
			foreach (var pi in target.GetType().GetProperties())
			{
				if (pi.GetIndexParameters().Length != 1)
				{
					continue;
				}

				pi.SetValue(target, value, new object[] { index });
				return true;
			}
			return false;
		}

		public static object CreateDefaultValue(Type type)
		{
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}
			else
			{
				var constructor = type.GetConstructor(new Type[] { });
				if (constructor == null)
				{
					return null;
				}

				return constructor.Invoke(null);
			}
		}
	}
}