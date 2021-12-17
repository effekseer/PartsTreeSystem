﻿using System;
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
			var env = new Environment();
			var commandManager = new PartsTreeSystem.CommandManager();

			var partsList = new PartsList();
			partsList.Renew();

			var nodeTreeGroup = new PartsTreeSystem.NodeTreeGroup();
			nodeTreeGroup.Init(typeof(NodeStruct), env);

			var nodeTree = PartsTreeSystem.Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);

			var configuration = new Altseed2.Configuration();
			configuration.EnabledCoreModules = Altseed2.CoreModules.Default | Altseed2.CoreModules.Tool;
			if (!Altseed2.Engine.Initialize("Example", 640, 480, configuration))
			{
				return;
			}

			Node selectedNode = null;
			Node popupedNode = null;

			while (Altseed2.Engine.DoEvents())
			{
				UpdateCommandPanel(env, ref commandManager, ref nodeTreeGroup, ref nodeTree);

				UpdateHistoryPanel(commandManager);

				UpdateNodeTreePanel(env, commandManager, nodeTreeGroup, nodeTree, ref selectedNode, ref popupedNode, partsList);

				if (selectedNode != null && nodeTree.FindInstance(selectedNode.InstanceID) == null)
				{
					selectedNode = null;
				}

				UpdateInspectorPanel(env, commandManager, nodeTreeGroup, nodeTree, selectedNode);

				Altseed2.Engine.Update();
			}

			Altseed2.Engine.Terminate();
		}

		private static void UpdateCommandPanel(PartsTreeSystem.Environment env, ref PartsTreeSystem.CommandManager commandManager, ref PartsTreeSystem.NodeTreeGroup nodeTreeGroup, ref PartsTreeSystem.NodeTree nodeTree)
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
					SaveNodeTreeGroup(nodeTreeGroup, env);
				}

				if (Altseed2.Engine.Tool.Button("Load"))
				{
					LoadNodeTreeGroup(env, ref commandManager, ref nodeTreeGroup, ref nodeTree);
				}
			}

			Altseed2.Engine.Tool.End();
		}

		static void SaveNodeTreeGroup(PartsTreeSystem.NodeTreeGroup nodeTreeGroup, PartsTreeSystem.Environment env)
		{
			var path = Altseed2.Engine.Tool.SaveDialog("nodes", System.IO.Directory.GetCurrentDirectory());
			if (!string.IsNullOrEmpty(path))
			{
				var text = nodeTreeGroup.Serialize(env);

				if (System.IO.Path.GetExtension(path) != ".nodes")
				{
					path += ".nodes";
				}

				System.IO.File.WriteAllText(path + ".nodes", text);
			}
		}

		static void LoadNodeTreeGroup(PartsTreeSystem.Environment env, ref PartsTreeSystem.CommandManager commandManager, ref PartsTreeSystem.NodeTreeGroup nodeTreeGroup, ref PartsTreeSystem.NodeTree nodeTree)
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

		private static void UpdateInspectorPanel(PartsTreeSystem.Environment env, PartsTreeSystem.CommandManager commandManager, PartsTreeSystem.NodeTreeGroup nodeTreeGroup, PartsTreeSystem.NodeTree nodeTree, Node selectedNode)
		{
			if (Altseed2.Engine.Tool.Begin("Inspector", Altseed2.ToolWindowFlags.NoCollapse))
			{
				if (selectedNode != null)
				{
					commandManager.StartEditFields(nodeTreeGroup, nodeTree, selectedNode, env);

					void updateFields(FieldGetterSetter getterSetter)
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
		}

		private static void UpdateNodeTreePanel(PartsTreeSystem.Environment env, PartsTreeSystem.CommandManager commandManager, PartsTreeSystem.NodeTreeGroup nodeTreeGroup, PartsTreeSystem.NodeTree nodeTree, ref Node selectedNode, ref Node popupedNode, PartsList partsList)
		{
			if (Altseed2.Engine.Tool.Begin("NodeTree", Altseed2.ToolWindowFlags.NoCollapse))
			{
				const string menuKey = "menu";

				void showNodePopup(Node node, ref Node popupedNode)
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

				void updateNode(Node node, ref Node selectedNode, ref Node popupedNode)
				{
					var n = node as NodeStruct;

					var flag = Altseed2.ToolTreeNodeFlags.OpenOnArrow;
					if (selectedNode == n)
					{
						flag |= Altseed2.ToolTreeNodeFlags.Selected;
					}

					if (Altseed2.Engine.Tool.TreeNodeEx(n.Name + "##" + node.InstanceID, flag))
					{
						if (Altseed2.Engine.Tool.IsItemClicked(Altseed2.ToolMouseButton.Left))
						{
							selectedNode = node;
						}

						showNodePopup(node, ref popupedNode);

						foreach (var child in node.Children)
						{
							updateNode(child, ref selectedNode, ref popupedNode);
						}
					}
					else
					{
						if (Altseed2.Engine.Tool.IsItemClicked(Altseed2.ToolMouseButton.Left))
						{
							selectedNode = node;
						}

						showNodePopup(node, ref popupedNode);
					}
				};

				updateNode(nodeTree.Root as Node, ref selectedNode, ref popupedNode);

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

					foreach (var p in partsList.Pathes)
					{
						if (Altseed2.Engine.Tool.Button(p))
						{
							var addingNodeTreeGroup = env.GetAsset(p) as PartsTreeSystem.NodeTreeGroup;

							if (addingNodeTreeGroup != null)
							{
								commandManager.AddNode(nodeTreeGroup, nodeTree, popupedNode.InstanceID, addingNodeTreeGroup, env);
							}

							Altseed2.Engine.Tool.CloseCurrentPopup();
						}
					}

					Altseed2.Engine.Tool.EndPopup();
				}
			}

			Altseed2.Engine.Tool.End();
		}

		private static void UpdateHistoryPanel(PartsTreeSystem.CommandManager commandManager)
		{
			if (Altseed2.Engine.Tool.Begin("History", Altseed2.ToolWindowFlags.NoCollapse))
			{
				for (int i = commandManager.Commands.Count() - 1; i >= Math.Max(0, commandManager.Commands.Count() - 20); i--)
				{
					var info = commandManager.Commands[i].GetInformation();

					var text = string.Empty;
					if (i == commandManager.CurrentCommandIndex)
					{
						text += "-";
					}
					text += info.Name;
					Altseed2.Engine.Tool.Text(text);
					Altseed2.Engine.Tool.Text(info.Detail);
				}
			}

			Altseed2.Engine.Tool.End();
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

	class Environment : PartsTreeSystem.Environment
	{
		Dictionary<PartsTreeSystem.Asset, string> pathes = new Dictionary<PartsTreeSystem.Asset, string>();
		public override PartsTreeSystem.Asset GetAsset(string path)
		{
			if (pathes.ContainsValue(path))
			{
				return pathes.Where(_ => _.Value == path).FirstOrDefault().Key;
			}
			var text = System.IO.File.ReadAllText(path);
			var nodeTreeGroup = PartsTreeSystem.NodeTreeGroup.Deserialize(text);

			pathes.Add(nodeTreeGroup, path);
			return nodeTreeGroup;
		}

		public override string GetAssetPath(PartsTreeSystem.Asset asset)
		{
			if (pathes.TryGetValue(asset, out var path))
			{
				return System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), path);
			}
			return System.IO.Directory.GetCurrentDirectory();
		}
	}

	class PartsList
	{
		public IReadOnlyCollection<string> Pathes { get { return pathes; } }

		string[] pathes = new string[0];

		public void Renew()
		{
			pathes = System.IO.Directory.GetFiles("./", "*.nodes");
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