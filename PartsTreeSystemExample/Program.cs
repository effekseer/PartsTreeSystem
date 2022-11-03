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
			var state = new EditorState();

			state.Env = new Environment();
			state.PartsList = new PartsList();
			state.PartsList.Renew();

			var context = new NodeTreeGroupContext();
			context.New(typeof(NodeStruct), state);

			var configuration = new Altseed2.Configuration();
			configuration.EnabledCoreModules = Altseed2.CoreModules.Default | Altseed2.CoreModules.Tool;
			if (!Altseed2.Engine.Initialize("Example", 640, 480, configuration))
			{
				return;
			}

			while (Altseed2.Engine.DoEvents())
			{
				UpdateMenu(ref context, state);

				UpdateHistoryPanel(context.CommandManager);

				UpdateNodeTreePanel(ref context, state);

				if (state.SelectedNode != null && context.NodeTree.FindInstance(state.SelectedNode.InstanceID) == null)
				{
					state.SelectedNode = null;
				}

				UpdateInspectorPanel(ref context, ref state);

				if (!string.IsNullOrEmpty(state.NextLoadPath))
				{
					context = new NodeTreeGroupContext();
					context.Load(state.NextLoadPath, state);
					state.Unselect();

					state.NextLoadPath = string.Empty;
				}

				Altseed2.Engine.Update();
			}

			Altseed2.Engine.Terminate();
		}

		private static void UpdateMenu(ref NodeTreeGroupContext context, EditorState state)
		{
			if (Altseed2.Engine.Tool.BeginMainMenuBar())
			{
				if (Altseed2.Engine.Tool.BeginMenu("File", true))
				{
					if (Altseed2.Engine.Tool.MenuItem("Load", string.Empty, false, true))
					{
						LoadNodeTreeGroup(ref context, state);
					}

					if (Altseed2.Engine.Tool.MenuItem("Save", string.Empty, false, true))
					{
						SaveNodeTreeGroup(ref context, state);
					}
					Altseed2.Engine.Tool.EndMenu();
				}

				if (Altseed2.Engine.Tool.BeginMenu("Edit", true))
				{
					if (Altseed2.Engine.Tool.MenuItem("Undo", string.Empty, false, true))
					{
						context.CommandManager.Undo(state.Env);
						context.EditorProperty.Rebuild();
					}

					if (Altseed2.Engine.Tool.MenuItem("Redo", string.Empty, false, true))
					{
						context.CommandManager.Redo(state.Env);
						context.EditorProperty.Rebuild();
					}

					Altseed2.Engine.Tool.EndMenu();
				}

				Altseed2.Engine.Tool.EndMainMenuBar();
			}
		}

		static void SaveNodeTreeGroup(ref NodeTreeGroupContext context, EditorState state)
		{
			var path = Altseed2.Engine.Tool.SaveDialog("nodes", System.IO.Directory.GetCurrentDirectory());
			if (!string.IsNullOrEmpty(path))
			{
				var text = context.NodeTreeGroup.Serialize(state.Env);
				var ext = System.IO.Path.GetExtension(path).ToLower();

				if (ext != ".nodes")
				{
					path += ".nodes";
				}

				System.IO.File.WriteAllText(path, text);
			}
		}

		static void LoadNodeTreeGroup(ref NodeTreeGroupContext context, EditorState state)
		{
			var path = Altseed2.Engine.Tool.OpenDialog("nodes", System.IO.Directory.GetCurrentDirectory());
			if (!string.IsNullOrEmpty(path))
			{
				state.NextLoadPath = path;
			}
		}

		private static void UpdateInspectorPanel(ref NodeTreeGroupContext context, ref EditorState state)
		{
			if (Altseed2.Engine.Tool.Begin("Inspector", Altseed2.ToolWindowFlags.NoCollapse))
			{
				if (state.SelectedNode != null)
				{
					context.CommandManager.StartEditFields(context.NodeTreeGroup, context.NodeTree, state.SelectedNode, state.Env);

					void updateFields(NodeTreeGroupContext context, Node selectedNode, PartsTreeSystem.ElementGetterSetterArray elementGetterSetterArray)
					{
						var prop = context.EditorProperty.Properties.FirstOrDefault(_ => _.InstanceID == selectedNode.InstanceID);
						bool isValueChanged = false;
						if (prop != null)
						{
							isValueChanged = prop.IsValueEdited(elementGetterSetterArray.Names);
						}

						var value = elementGetterSetterArray.GetValue();
						var name = elementGetterSetterArray.GetName();

						if (isValueChanged)
						{
							name = "*" + name;
						}
						else
						{
							name = " " + name;
						}

						if (value is string)
						{
							var s = (string)value;

							var result = Altseed2.Engine.Tool.InputText(name, s, 200, Altseed2.ToolInputTextFlags.None);
							if (result != null)
							{
								elementGetterSetterArray.SetValue(result);
								context.CommandManager.NotifyEditFields(selectedNode);
							}
						}
						if (value is int)
						{
							var v = (int)value;

							if (Altseed2.Engine.Tool.DragInt(name, ref v, 1, -100, 100, "%d", Altseed2.ToolSliderFlags.None))
							{
								elementGetterSetterArray.SetValue(v);
								context.CommandManager.NotifyEditFields(selectedNode);
							}
						}
						else if (value is float)
						{
							var v = (float)value;

							if (Altseed2.Engine.Tool.DragFloat(name, ref v, 1, -100, 100, "%f", Altseed2.ToolSliderFlags.None))
							{
								elementGetterSetterArray.SetValue(v);
								context.CommandManager.NotifyEditFields(selectedNode);
							}
						}
						else if (value is Vector3)
						{
							var v = (Vector3)value;

							var fields = v.GetType().GetFields();

							foreach (var field in fields)
							{
								elementGetterSetterArray.Push(v, field);
								updateFields(context, selectedNode, elementGetterSetterArray);
								elementGetterSetterArray.Pop();
							}
						}
						else if (value is Parameter)
						{
							var v = (Parameter)value;

							var fields = v.GetType().GetFields();

							foreach (var field in fields)
							{
								elementGetterSetterArray.Push(v, field);
								updateFields(context, selectedNode, elementGetterSetterArray);
								elementGetterSetterArray.Pop();
							}
						}
						else if (value is Array array)
						{
							var count = array.Length;
							if (Altseed2.Engine.Tool.DragInt(name, ref count, 1, 0, 100, "%d", Altseed2.ToolSliderFlags.None))
							{
								var next = PartsTreeSystem.EditorUtility.ResizeArray(array, count);
								elementGetterSetterArray.SetValue(next);
								context.CommandManager.NotifyEditFields(selectedNode);
							}

							for (int i = 0; i < array.Length; i++)
							{
								elementGetterSetterArray.Push(array, i);
								updateFields(context, selectedNode, elementGetterSetterArray);
								elementGetterSetterArray.Pop();
							}
						}
						else if (value is IList)
						{
							var v = (IList)value;
							var count = v.Count;
							if (Altseed2.Engine.Tool.DragInt(name, ref count, 1, 0, 100, "%d", Altseed2.ToolSliderFlags.None))
							{
								PartsTreeSystem.EditorUtility.ResizeList(v, count);

								context.CommandManager.NotifyEditFields(selectedNode);
							}

							for (int i = 0; i < v.Count; i++)
							{
								elementGetterSetterArray.Push(v, i);
								updateFields(context, selectedNode, elementGetterSetterArray);
								elementGetterSetterArray.Pop();
							}
						}
						else
						{
							Altseed2.Engine.Tool.Text(name);
						}
					};

					var fields = state.SelectedNode.GetType().GetFields();

					var elementGetterSetterArray = new PartsTreeSystem.ElementGetterSetterArray();
					foreach (var field in fields)
					{
						elementGetterSetterArray.Push(state.SelectedNode, field);
						updateFields(context, state.SelectedNode, elementGetterSetterArray);
						elementGetterSetterArray.Pop();
					}

					context.CommandManager.EndEditFields(state.SelectedNode, state.Env);
				}

				if (!Altseed2.Engine.Tool.IsAnyItemActive())
				{
					context.CommandManager.SetFlagToBlockMergeCommands();
				}
			}

			Altseed2.Engine.Tool.End();
		}

		private static void UpdateNodeTreePanel(ref NodeTreeGroupContext context, EditorState state)
		{
			if (Altseed2.Engine.Tool.Begin("NodeTree", Altseed2.ToolWindowFlags.NoCollapse))
			{
				var delayEvents = new List<Action>();

				const string menuKey = "menu";

				var commandManager = context.CommandManager;
				var nodeTreeGroup = context.NodeTreeGroup;
				var nodeTree = context.NodeTree;
				var partsList = state.PartsList;
				var env = state.Env;
				var nodeTreeGroupEditorProperty = context.EditorProperty;

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

					if (popupedNode == node)
					{
						var prop = nodeTreeGroupEditorProperty.Properties.FirstOrDefault(_ => _.InstanceID == node.InstanceID);

						if (Altseed2.Engine.Tool.BeginPopup(menuKey, Altseed2.ToolWindowFlags.None))
						{
							if (Altseed2.Engine.Tool.MenuItem("Add Node", "", false, true))
							{
								var instanceID = popupedNode.InstanceID;
								delayEvents.Add(() =>
								{
									commandManager.AddNode(nodeTreeGroup, nodeTree, instanceID, typeof(NodeStruct), env);
									nodeTreeGroupEditorProperty.Rebuild();
								});
								Altseed2.Engine.Tool.CloseCurrentPopup();
							}

							if (Altseed2.Engine.Tool.BeginMenu("Add Node with Parts", true))
							{
								foreach (var p in partsList.Pathes)
								{
									if (Altseed2.Engine.Tool.MenuItem(p, "", false, true))
									{
										var addingNodeTreeAsset = env.GetAsset(p) as PartsTreeSystem.NodeTreeAsset;

										if (addingNodeTreeAsset != null)
										{
											var instanceID = popupedNode.InstanceID;
											delayEvents.Add(() =>
											{
												commandManager.AddNode(nodeTreeGroup, nodeTree, instanceID, addingNodeTreeAsset, env);
												nodeTreeGroupEditorProperty.Rebuild();
											});
										}

										Altseed2.Engine.Tool.CloseCurrentPopup();
									}
								}

								Altseed2.Engine.Tool.EndMenu();
							}

							if (nodeTreeGroup.CanRemoveNode(popupedNode.InstanceID, env))
							{
								if (Altseed2.Engine.Tool.MenuItem("Remove Node", "", false, true))
								{
									var instanceID = popupedNode.InstanceID;
									delayEvents.Add(() =>
									{
										commandManager.RemoveNode(nodeTreeGroup, nodeTree, instanceID, env);
										nodeTreeGroupEditorProperty.Rebuild();
									});
									Altseed2.Engine.Tool.CloseCurrentPopup();
								}
							}

							if (Altseed2.Engine.Tool.MenuItem("Copy Node", "", false, true))
							{
								var instanceID = popupedNode.InstanceID;
								delayEvents.Add(() =>
								{
									var text = nodeTreeGroup.Copy(instanceID, env);
									Altseed2.Engine.Tool.SetClipboardText(text);
								});
								Altseed2.Engine.Tool.CloseCurrentPopup();
							}

							if (Altseed2.Engine.Tool.MenuItem("Paste Node", "", false, true))
							{
								var instanceID = popupedNode.InstanceID;
								delayEvents.Add(() =>
								{
									commandManager.Paste(nodeTreeGroup, nodeTree, instanceID, Altseed2.Engine.Tool.GetClipboardText(), env);
									nodeTreeGroupEditorProperty.Rebuild();
								});
								Altseed2.Engine.Tool.CloseCurrentPopup();
							}

							if (prop.Generator is PartsTreeSystem.Asset)
							{
								var path = env.GetAssetPath(prop.Generator as PartsTreeSystem.Asset);
								if (Altseed2.Engine.Tool.MenuItem("Edit Parts", "", false, true))
								{
									state.NextLoadPath = path;
									Altseed2.Engine.Tool.CloseCurrentPopup();
								}
							}

							Altseed2.Engine.Tool.EndPopup();
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

					string parts = string.Empty;
					var prop = nodeTreeGroupEditorProperty.Properties.FirstOrDefault(_ => _.InstanceID == node.InstanceID);
					if (prop.Generator is PartsTreeSystem.Asset)
					{
						parts += "(Parts)";
					}

					if (Altseed2.Engine.Tool.TreeNodeEx(n.Name + parts + "##" + node.InstanceID, flag))
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

						Altseed2.Engine.Tool.TreePop();
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

				updateNode(nodeTree.Root as Node, ref state.SelectedNode, ref state.PopupedNode);

				foreach (var e in delayEvents)
				{
					e();
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

		public class NodeTreeGroupContext
		{
			public PartsTreeSystem.NodeTreeAsset NodeTreeGroup;
			public PartsTreeSystem.NodeTree NodeTree;
			public PartsTreeSystem.NodeTreeAssetEditorProperty EditorProperty;
			public PartsTreeSystem.CommandManager CommandManager;

			public void New(Type type, EditorState state)
			{
				NodeTreeGroup = new PartsTreeSystem.NodeTreeAsset();
				NodeTreeGroup.Init(type, state.Env);
				EditorProperty = new PartsTreeSystem.NodeTreeAssetEditorProperty(NodeTreeGroup, state.Env);
				NodeTree = PartsTreeSystem.Utility.CreateNodeFromNodeTreeGroup(NodeTreeGroup, state.Env);
				CommandManager = new PartsTreeSystem.CommandManager();
			}

			public void Load(string path, EditorState state)
			{
				var text = System.IO.File.ReadAllText(path);
				NodeTreeGroup = PartsTreeSystem.NodeTreeAsset.Deserialize(text, state.Env);
				EditorProperty = new PartsTreeSystem.NodeTreeAssetEditorProperty(NodeTreeGroup, state.Env);
				NodeTree = PartsTreeSystem.Utility.CreateNodeFromNodeTreeGroup(NodeTreeGroup, state.Env);
				CommandManager = new PartsTreeSystem.CommandManager();
			}
		}

		public class EditorState
		{
			public PartsTreeSystem.Environment Env;
			public PartsList PartsList;
			public Node SelectedNode = null;
			public Node PopupedNode = null;
			public string NextLoadPath = string.Empty;

			public void Unselect()
			{
				SelectedNode = null;
				PopupedNode = null;
			}
		}

		public class NodeStruct : Node
		{
			public string Name = "Node";
			public int Value1;
			public float Value2;
			public Vector3 Value3;
			public Parameter Param1 = new Parameter();
			public List<int> List1 = new List<int>();
			public int[] Array1 = new int[0];
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

		public void InsertChild(int index, PartsTreeSystem.INode node)
		{
			Children.Insert(index, node as Node);
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
			var nodeTreeGroup = PartsTreeSystem.NodeTreeAsset.Deserialize(text, this);

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

	public struct Vector3
	{
		public float X;
		public float Y;
		public float Z;
	}

	public class Parameter
	{
		public float Param1;
		public float Param2;
		public float Param3;
	}
}