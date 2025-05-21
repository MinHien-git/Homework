using UnityEngine;

public abstract class BTNode
{
    public abstract bool Execute();
}

public class Selector : BTNode
{
    private BTNode[] children;

    public Selector(params BTNode[] children) => this.children = children;

    public override bool Execute()
    {
        foreach (var child in children)
            if (child.Execute())
                return true;
        return false;
    }
}

public class Sequence : BTNode
{
    private BTNode[] children;

    public Sequence(params BTNode[] children) => this.children = children;

    public override bool Execute()
    {
        foreach (var child in children)
            if (!child.Execute())
                return false;
        return true;
    }
}

public class Leaf : BTNode
{
    private System.Func<bool> action;

    public Leaf(System.Func<bool> action) => this.action = action;

    public override bool Execute() => action();
}
