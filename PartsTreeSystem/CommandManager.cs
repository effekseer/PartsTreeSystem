using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	public class CommandInformation
	{
		public string Name;
		public string Detail;
	}

	public class Command
	{
		virtual public void Execute(Environment env) { }
		virtual public void Unexecute(Environment env) { }

		virtual public CommandInformation GetInformation() { return null; }
	}

	public class CommandCollectionCommand : Command
	{
		Command[] commands;

		public CommandCollectionCommand(IEnumerable<Command> commands)
		{
			this.commands = commands.ToArray();
		}

		public override void Execute(Environment env)
		{
			foreach (var c in commands)
			{
				c.Execute(env);
			}
		}

		public override void Unexecute(Environment env)
		{
			foreach (var c in commands.Reverse())
			{
				c.Unexecute(env);
			}
		}

		public override CommandInformation GetInformation()
		{
			var info = new CommandInformation();
			info.Name = "Collection";

			info.Detail = string.Join("\n", commands.Select(_ => { return _.GetInformation().Name; }));

			return base.GetInformation();
		}
	}

	public class DelegateCommand : Command
	{
		public Action OnExecute;
		public Action OnUnexecute;
		public string Name;
		public string Detail;

		public override void Execute(Environment env)
		{
			OnExecute?.Invoke();
		}

		public override void Unexecute(Environment env)
		{
			OnUnexecute?.Invoke();
		}

		public override CommandInformation GetInformation()
		{
			return new CommandInformation { Name = Name, Detail = Detail };
		}
	}

	class ValueChangeCommand : Command
	{
		public Asset Asset;
		public IInstanceContainer Container;
		public int InstanceID { get; set; }

		public Difference DiffRedo;

		public Difference DiffUndo;

		public Difference NewDifference;

		public Difference OldDifference;

		public override void Execute(Environment env)
		{
			var instance = Container.FindInstance(InstanceID);
			if (instance != null)
			{
				object obj = instance;
				Difference.ApplyDifference(ref obj, DiffRedo, Asset, Container, env);
			}

			Asset.SetDifference(InstanceID, NewDifference);
		}
		public override void Unexecute(Environment env)
		{
			var instance = Container.FindInstance(InstanceID);
			if (instance != null)
			{
				object obj = instance;
				Difference.ApplyDifference(ref obj, DiffUndo, Asset, Container, env);
			}

			Asset.SetDifference(InstanceID, OldDifference);
		}

		public static ValueChangeCommand Merge(ValueChangeCommand first, ValueChangeCommand second)
		{
			if (first.Asset != second.Asset ||
			first.Container != second.Container ||
			first.InstanceID != second.InstanceID)
			{
				return null;
			}

			var keys1 = first.DiffRedo.Modifications.Select(_ => _.Target);
			var keys2 = second.DiffRedo.Modifications.Select(_ => _.Target);


			if (keys1.Count() == keys2.Count() && keys1.Union(keys2).Count() == keys2.Count())
			{
				var cmd = new ValueChangeCommand();

				cmd.Container = first.Container;
				cmd.InstanceID = first.InstanceID;
				cmd.Asset = first.Asset;
				cmd.DiffRedo = second.DiffRedo;
				cmd.DiffUndo = first.DiffUndo;
				cmd.OldDifference = first.OldDifference;
				cmd.NewDifference = second.NewDifference;
				return cmd;
			}

			return null;
		}

		public override CommandInformation GetInformation()
		{
			return new CommandInformation { Name = "ValueChange", Detail = string.Join("\n", DiffRedo.Modifications.Select(_ => _.Target.ToString())) };
		}
	}

	public class CommandManager
	{
		class EditFieldState
		{
			public Asset Asset;
			public IInstanceContainer Container;
			public IInstance Target;
			public bool IsEdited = false;
			public FieldState State = new FieldState();
		}

		Dictionary<object, EditFieldState> editFieldStates = new Dictionary<object, EditFieldState>();

		bool blockMerge = false;

		int currentCommand = -1;

		List<Command> commands = new List<Command>();

		int mergingBlockCount = 0;

		List<Command> mergingBlock = new List<Command>();

		public IReadOnlyList<Command> Commands
		{
			get
			{
				return commands;
			}
		}

		public int CurrentCommandIndex
		{
			get
			{
				return currentCommand;
			}
		}

		public bool IsInMergingBlock
		{
			get
			{
				return mergingBlockCount > 0;
			}
		}

		public void AddCommand(Command command)
		{
			if (IsInMergingBlock)
			{
				if (mergingBlock.Count > 0)
				{
					var newCommand = TryMergeCommand(mergingBlock.Last(), command);
					if (newCommand != null)
					{
						mergingBlock[mergingBlock.Count - 1] = newCommand;
					}
					else
					{
						mergingBlock.Add(command);
					}
				}
				else
				{
					mergingBlock.Add(command);
				}
			}
			else
			{
				if (TryMergeCommand(command))
				{
					return;
				}

				var count = commands.Count - (currentCommand + 1);
				if (count > 0)
				{
					commands.RemoveRange(currentCommand + 1, count);
				}
				commands.Add(command);
				currentCommand += 1;
				blockMerge = false;
			}
		}

		Command TryMergeCommand(Command command1, Command command2)
		{
			if (command1 is ValueChangeCommand vc1 && command2 is ValueChangeCommand vc2)
			{
				var newCommand = ValueChangeCommand.Merge(vc1, vc2);
				return newCommand;
			}

			return null;
		}

		bool TryMergeCommand(Command command)
		{
			if (blockMerge || currentCommand < 0)
			{
				return false;
			}

			var newCommand = TryMergeCommand(commands[currentCommand], command);

			if (newCommand != null)
			{
				ReplaceLastCommand(newCommand);
				return true;
			}

			return false;
		}

		void ReplaceLastCommand(Command command)
		{
			if (commands.Count == 0 || currentCommand < 0)
			{
				throw new InvalidOperationException();
			}

			var count = commands.Count - (currentCommand);
			if (count > 0)
			{
				commands.RemoveRange(currentCommand, count);
			}
			commands.Add(command);
		}


		public void Undo(Environment env)
		{
			if (currentCommand >= 0)
			{
				commands[currentCommand].Unexecute(env);
				currentCommand--;
			}

			SetFlagToBlockMergeCommands();
		}

		public void Redo(Environment env)
		{
			if (currentCommand + 1 < commands.Count)
			{
				commands[currentCommand + 1].Execute(env);
				currentCommand++;
			}

			SetFlagToBlockMergeCommands();
		}

		void AddNodeInternal(NodeTreeAsset nodeTreeGroup, NodeTree nodeTree, int parentID, Environment env, string before, int newNodeID, string after, string commandName)
		{
			void execute()
			{
				var parentNode = nodeTree.FindInstance(parentID) as INode;
				var newNodeTree = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);
				var newNode = newNodeTree.FindInstance(newNodeID);
				parentNode.AddChild(newNode as INode);
			};

			execute();

			var command = new DelegateCommand();
			command.OnExecute = () =>
			{
				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(after, env);
				execute();
			};

			command.OnUnexecute = () =>
			{
				var parent = nodeTree.FindParent(newNodeID);
				if (parent != null)
				{
					parent.RemoveChild(newNodeID);
				}

				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(before, env);
			};

			command.Name = commandName;
			command.Detail = string.Empty;

			AddCommand(command);
		}

		/// <summary>
		/// Add a node
		/// </summary>
		/// <param name="nodeTreeGroup"></param>
		/// <param name="nodeTree"></param>
		/// <param name="parentID"></param>
		/// <param name="addingNodeTreeGroup"></param>
		/// <param name="env"></param>
		/// <returns>InstanceID of added node</returns>
		public int AddNode(NodeTreeAsset nodeTreeGroup, NodeTree nodeTree, int parentID, NodeTreeAsset addingNodeTreeGroup, Environment env)
		{
			var before = nodeTreeGroup.InternalData.Serialize(env);
			var newNodeID = nodeTreeGroup.AddNodeTreeGroup(parentID, addingNodeTreeGroup, env);
			var after = nodeTreeGroup.InternalData.Serialize(env);
			AddNodeInternal(nodeTreeGroup, nodeTree, parentID, env, before, newNodeID, after, "AddNode(NodeTreeGroup)");
			return newNodeID;
		}

		/// <summary>
		/// Add a node
		/// </summary>
		/// <param name="nodeTreeGroup"></param>
		/// <param name="nodeTree"></param>
		/// <param name="parentID"></param>
		/// <param name="type"></param>
		/// <param name="env"></param>
		/// <returns>InstanceID of added node</returns>
		public int AddNode(NodeTreeAsset nodeTreeGroup, NodeTree nodeTree, int parentID, Type type, Environment env)
		{
			var before = nodeTreeGroup.InternalData.Serialize(env);
			var newNodeID = nodeTreeGroup.AddNode(parentID, type, env);
			var after = nodeTreeGroup.InternalData.Serialize(env);
			AddNodeInternal(nodeTreeGroup, nodeTree, parentID, env, before, newNodeID, after, "AddNode");
			return newNodeID;
		}

		public void RemoveNode(NodeTreeAsset nodeTreeGroup, NodeTree nodeTree, int nodeID, Environment env)
		{
			var parentNode = nodeTree.FindParent(nodeID);
			var parentNodeID = parentNode.InstanceID;

			var before = nodeTreeGroup.InternalData.Serialize(env);
			if (!nodeTreeGroup.RemoveNode(nodeID, env))
			{
				return;
			}

			var after = nodeTreeGroup.InternalData.Serialize(env);

			void execute()
			{
				var currentParentNode = nodeTree.FindInstance(parentNodeID) as INode;
				currentParentNode.RemoveChild(nodeID);
			};

			execute();

			var command = new DelegateCommand();
			command.OnExecute = () =>
			{
				execute();
				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(after, env);
			};

			command.OnUnexecute = () =>
			{
				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(before, env);

				var pn = nodeTree.FindInstance(parentNodeID) as INode;
				var newNodeTree = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);
				var newNode = newNodeTree.FindInstance(nodeID);
				pn.AddChild(newNode as INode);
			};

			command.Name = "RemoveNode";
			command.Detail = string.Empty;

			AddCommand(command);
		}

		public void MoveNode(NodeTreeAsset nodeTreeGroup, NodeTree nodeTree, int nodeID, int insertedParentNodeID, int index, Environment env)
		{
			if (!nodeTreeGroup.CanRemoveNode(nodeID, env))
			{
				return;
			}

			var targetNode = nodeTree.FindInstance(nodeID) as INode;
			var insertedParentNode = nodeTree.FindInstance(insertedParentNodeID) as INode;

			if (targetNode == null || insertedParentNode == null)
			{
				return;
			}

			if (index < 0 || index > insertedParentNode.GetChildren().Count)
			{
				return;
			}

			var before = nodeTreeGroup.InternalData.Serialize(env);

			var insertingNodeBase = nodeTreeGroup.InternalData.Bases.FirstOrDefault(_ => _.IDRemapper.ContainsValue(nodeID));

			var previoudParentNodeID = insertingNodeBase.ParentID;

			var sortedNodeGroups = new Dictionary<int, List<NodeTreeBase>>();
			foreach (var b in nodeTreeGroup.InternalData.Bases)
			{
				if (!sortedNodeGroups.ContainsKey(b.ParentID))
				{
					sortedNodeGroups.Add(b.ParentID, new List<NodeTreeBase>());
				}

				sortedNodeGroups[b.ParentID].Add(b);
			}

			int originalIndex = -1;
			foreach (var b in sortedNodeGroups)
			{
				if (b.Key == insertingNodeBase.ParentID)
				{
					originalIndex = b.Value.IndexOf(insertingNodeBase);
					b.Value.Remove(insertingNodeBase);
				}
			}

			insertingNodeBase.ParentID = insertedParentNodeID;

			sortedNodeGroups[insertedParentNodeID].Insert(index, insertingNodeBase);

			var sortedBases = new List<NodeTreeBase>();
			const int rootNodeID = -1;
			var root = sortedNodeGroups.FirstOrDefault(_ => _.Key == rootNodeID);
			sortedBases.AddRange(root.Value);
			sortedNodeGroups.Remove(-1);

			while (sortedNodeGroups.Count > 0)
			{
				var keys = sortedNodeGroups.Keys.ToArray();

				foreach (var key in keys)
				{
					if (sortedBases.Any(_ => _.IDRemapper.ContainsValue(key)))
					{
						sortedBases.AddRange(sortedNodeGroups[key]);
						sortedNodeGroups.Remove(key);
					}
				}
			}

			nodeTreeGroup.InternalData.Bases = sortedBases.ToList();

			var after = nodeTreeGroup.InternalData.Serialize(env);

			void execute()
			{
				var node = nodeTree.FindInstance(nodeID) as INode;
				var insertedParentNodeInner = nodeTree.FindInstance(insertedParentNodeID) as INode;
				var previousParentNodeInner = nodeTree.FindInstance(previoudParentNodeID) as INode;
				previousParentNodeInner.RemoveChild(nodeID);
				insertedParentNodeInner.InsertChild(index, node);
			};

			execute();

			var command = new DelegateCommand();
			command.OnExecute = () =>
			{
				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(after, env);
				execute();
			};

			command.OnUnexecute = () =>
			{
				var previousNodeParent = nodeTree.FindInstance(previoudParentNodeID) as INode;
				var parentNode = nodeTree.FindInstance(insertedParentNodeID) as INode;
				var node = nodeTree.FindInstance(nodeID) as INode;
				if (previousNodeParent != null)
				{
					parentNode.RemoveChild(nodeID);
					previousNodeParent.InsertChild(originalIndex, node);
				}

				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(before, env);
			};

			command.Name = "MoveNode";
			command.Detail = string.Empty;

			AddCommand(command);

		}

		public void Paste(NodeTreeAsset nodeTreeGroup, NodeTree nodeTree, int instanceID, string data, Environment env)
		{
			var before = nodeTreeGroup.InternalData.Serialize(env);
			var newNodeID = nodeTreeGroup.Paste(data, instanceID, env);
			var after = nodeTreeGroup.InternalData.Serialize(env);

			void execute()
			{
				var selfNode = nodeTree.FindInstance(instanceID) as INode;
				var parentNode = nodeTree.FindParent(selfNode.InstanceID) as INode;
				var newNodeTree = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);
				var newNode = newNodeTree.FindInstance(newNodeID);

				parentNode.InsertChild(parentNode.GetChildren().ToList().IndexOf(selfNode), newNode as INode);
				parentNode.RemoveChild(selfNode.InstanceID);
			};

			execute();

			var command = new DelegateCommand();
			command.OnExecute = () =>
			{
				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(after, env);
				execute();
			};

			command.OnUnexecute = () =>
			{
				var parent = nodeTree.FindParent(newNodeID);

				nodeTreeGroup.InternalData = NodeTreeAssetInternalData.Deserialize(before, env);
				var newNodeTree = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);
				var selfNode = newNodeTree.FindInstance(instanceID) as INode;
				var newNode = nodeTree.FindInstance(newNodeID);

				if (parent != null)
				{
					parent.InsertChild(parent.GetChildren().ToList().IndexOf(newNode as INode), selfNode as INode);
					parent.RemoveChild(newNodeID);
				}
			};

			command.Name = "Paste";
			command.Detail = string.Empty;

			AddCommand(command);
		}

		public void StartEditFields(Asset asset, IInstanceContainer instanceContainer, IInstance editedInstance, Environment environment)
		{
			var state = new EditFieldState { Target = editedInstance, Asset = asset, Container = instanceContainer };
			state.State.Store(editedInstance, environment);
			editFieldStates.Add(editedInstance, state);
		}

		public void NotifyEditFields(IInstance instance)
		{
			if (editFieldStates.TryGetValue(instance, out var v))
			{
				v.IsEdited = true;
			}
		}

		public bool EndEditFields(IInstance o, Environment environment)
		{
			if (editFieldStates.TryGetValue(o, out var v))
			{
				if (v.IsEdited)
				{
					var fs = new FieldState();
					fs.Store(o, environment);
					var diffUndo = v.State.GenerateDifference(fs);
					var diffRedo = fs.GenerateDifference(v.State);

					var instanceID = v.Target.InstanceID;
					var asset = v.Asset;
					var container = v.Container;

					var oldDifference = asset.GetDifference(instanceID);

					Difference newDifference;
					if (oldDifference != null)
					{
						newDifference = Difference.MergeDifference(diffRedo, oldDifference);
					}
					else
					{
						newDifference = diffRedo;
					}

					asset.SetDifference(instanceID, newDifference);

					var command = new ValueChangeCommand();

					command.Asset = asset;
					command.Container = container;
					command.InstanceID = instanceID;
					command.DiffRedo = diffRedo;
					command.DiffUndo = diffUndo;
					command.NewDifference = newDifference;
					command.OldDifference = oldDifference;
					AddCommand(command);
				}

				editFieldStates.Remove(o);

				return v.IsEdited;
			}

			return false;
		}

		public void SetFlagToBlockMergeCommands()
		{
			blockMerge = true;
		}

		public void PushMergingBlock()
		{
			mergingBlockCount++;
		}

		public void PopMergingBlock()
		{
			if (mergingBlockCount == 0)
			{
				throw new InvalidOperationException();
			}

			mergingBlockCount--;
			if (mergingBlockCount == 0)
			{
				if (mergingBlock.Count > 0)
				{
					var cmd = new CommandCollectionCommand(mergingBlock);
					mergingBlock.Clear();
					AddCommand(cmd);
				}
			}
		}
	}
}