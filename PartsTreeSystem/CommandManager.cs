using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	public class Command
	{
		virtual public void Execute(Environment env) { }
		virtual public void Unexecute(Environment env) { }
	}

	public class DelegateCommand : Command
	{
		public Action OnExecute;
		public Action OnUnexecute;

		public override void Execute(Environment env)
		{
			OnExecute?.Invoke();
		}

		public override void Unexecute(Environment env)
		{
			OnUnexecute?.Invoke();
		}
	}

	class ValueChangeCommand : Command
	{
		public Asset Asset;
		public IAssetInstanceRoot Root;
		public int InstanceID { get; set; }

		public Difference DiffRedo;

		public Difference DiffUndo;

		public Difference NewDifference;

		public Difference OldDifference;

		public override void Execute(Environment env)
		{
			var instance = Root.FindInstance(InstanceID);
			if (instance != null)
			{
				object obj = instance;
				Difference.ApplyDifference(ref obj, DiffRedo, Asset, Root, env);
			}

			Asset.SetDifference(InstanceID, NewDifference);
		}
		public override void Unexecute(Environment env)
		{
			var instance = Root.FindInstance(InstanceID);
			if (instance != null)
			{
				object obj = instance;
				Difference.ApplyDifference(ref obj, DiffUndo, Asset, Root, env);
			}

			Asset.SetDifference(InstanceID, OldDifference);
		}

		public static ValueChangeCommand Merge(ValueChangeCommand first, ValueChangeCommand second)
		{
			if (first.Asset != second.Asset ||
			first.Root != second.Root ||
			first.InstanceID != second.InstanceID)
			{
				return null;
			}

			var keys1 = first.DiffRedo.Modifications.Select(_ => _.Target);
			var keys2 = second.DiffRedo.Modifications.Select(_ => _.Target);


			if (keys1.Count() == keys2.Count() && keys1.Union(keys2).Count() == keys2.Count())
			{
				var cmd = new ValueChangeCommand();

				cmd.Root = first.Root;
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
	}

	public class CommandManager
	{
		class EditFieldState
		{
			public Asset Asset;
			public IAssetInstanceRoot Root;
			public IInstanceID Target;
			public bool IsEdited = false;
			public FieldState State = new FieldState();
		}

		Dictionary<object, EditFieldState> editFieldStates = new Dictionary<object, EditFieldState>();

		bool blockMerge = false;

		int currentCommand = -1;

		List<Command> commands = new List<Command>();

		public void AddCommand(Command command)
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

		bool TryMergeCommand(Command command)
		{
			if (blockMerge || currentCommand < 0)
			{
				return false;
			}

			if (command is ValueChangeCommand vc && commands[currentCommand] is ValueChangeCommand lastCommand)
			{
				var newCommand = ValueChangeCommand.Merge(lastCommand, vc);
				if (newCommand != null)
				{
					ReplaceLastCommand(newCommand);
					return true;
				}
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

		public void AddNode(NodeTreeGroup nodeTreeGroup, NodeTree nodeTree, int parentID, Type type, Environment env)
		{
			var before = nodeTreeGroup.InternalData.Serialize();
			var newNodeID = nodeTreeGroup.AddNode(parentID, type, env);
			var after = nodeTreeGroup.InternalData.Serialize();

			Action execute = () =>
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
				nodeTreeGroup.InternalData = NodeTreeGroupInternalData.Deserialize(after);
				execute();
			};

			command.OnUnexecute = () =>
			{
				var parent = nodeTree.FindParent(newNodeID);
				if (parent != null)
				{
					parent.RemoveChild(newNodeID);
				}

				nodeTreeGroup.InternalData = NodeTreeGroupInternalData.Deserialize(before);
			};

			AddCommand(command);
		}

		public void RemoveNode(NodeTreeGroup nodeTreeGroup, NodeTree nodeTree, int nodeID, Environment env)
		{
			var parentNode = nodeTree.FindParent(nodeID) as INode;
			var parentNodeID = parentNode.InstanceID;

			var before = nodeTreeGroup.InternalData.Serialize();
			if (!nodeTreeGroup.RemoveNode(nodeID))
			{
				return;
			}

			var after = nodeTreeGroup.InternalData.Serialize();

			Action execute = () =>
			{
				var currentParentNode = nodeTree.FindInstance(parentNodeID) as INode;
				currentParentNode.RemoveChild(nodeID);
			};

			execute();

			var command = new DelegateCommand();
			command.OnExecute = () =>
			{
				execute();
				nodeTreeGroup.InternalData = NodeTreeGroupInternalData.Deserialize(after);
			};

			command.OnUnexecute = () =>
			{
				nodeTreeGroup.InternalData = NodeTreeGroupInternalData.Deserialize(before);

				var parentNode = nodeTree.FindInstance(parentNodeID) as INode;
				var newNodeTree = Utility.CreateNodeFromNodeTreeGroup(nodeTreeGroup, env);
				var newNode = newNodeTree.FindInstance(nodeID);
				parentNode.AddChild(newNode as INode);
			};

			AddCommand(command);
		}

		public void StartEditFields(Asset asset, IAssetInstanceRoot root, IInstanceID o, Environment env)
		{
			var state = new EditFieldState { Target = o, Asset = asset, Root = root };
			state.State.Store(o, env);
			editFieldStates.Add(o, state);
		}

		public void NotifyEditFields(IInstanceID o)
		{
			if (editFieldStates.TryGetValue(o, out var v))
			{
				v.IsEdited = true;
			}
		}

		public bool EndEditFields(IInstanceID o, Environment env)
		{
			if (editFieldStates.TryGetValue(o, out var v))
			{
				if (v.IsEdited)
				{
					var fs = new FieldState();
					fs.Store(o, env);
					var diffUndo = v.State.GenerateDifference(fs);
					var diffRedo = fs.GenerateDifference(v.State);

					var instanceID = v.Target.InstanceID;
					var asset = v.Asset;
					var root = v.Root;

					var oldDifference = asset.GetDifference(instanceID);

					Difference newDifference = null;

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
					command.Root = root;
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
	}
}