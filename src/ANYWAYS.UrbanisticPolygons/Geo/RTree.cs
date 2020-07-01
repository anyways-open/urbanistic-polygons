using System;
using System.Collections;
using System.Collections.Generic;

namespace ANYWAYS.UrbanisticPolygons.Geo
{
    /// <summary>
    /// R-tree implementation of a spatial index.
    /// http://en.wikipedia.org/wiki/R-tree
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class RTree<T> : IEnumerable<T>
    {
        private int _count; // Holds the number of objects in this index.
        private Node _root; // Holds the root node.
        private readonly int _maxLeafSize = 200; // Holds the maximum leaf size M.
        private readonly int _minLeafSize = 100; // Holds the minimum leaf size m.

        /// <summary>
        /// Creates a new index.
        /// </summary>
        public RTree()
        {

        }

        /// <summary>
        /// Returns the number of objects in this index.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Adds a new item with the corresponding box.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="item"></param>
		public void Add(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box, T item)
        {
            _count++;

            if (_root == null)
            { // create the root.
                _root = new Node();
				_root.Boxes = new List<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)>();
                _root.Children = new List<T>();
            }

            // add new data.
            var leaf = RTree<T>.ChooseLeaf(_root, box);
            var newRoot = RTree<T>.Add(leaf, box, item, _minLeafSize, _maxLeafSize);
            if (newRoot != null)
            { // there should be a new root.
                _root = newRoot;
            }
        }

        /// <summary>
        /// Removes the given item.
        /// </summary>
        /// <param name="item"></param>
        public void Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the given item when it is contained in the given box.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="item"></param>
        public void Remove(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box, T item)
        {
            if(RTree<T>.RemoveSimple(_root, box, item))
            {
                _count--;
            }
        }

        /// <summary>
        /// Queries this index and returns all objects with overlapping bounding boxes.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public IEnumerable<T> Get(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            var result = new HashSet<T>();
            RTree<T>.Get(_root, box, result);
            return result;
        }

        /// <summary>
        /// Cancels the current request.
        /// </summary>
        public void GetCancel()
        {

        }

        /// <summary>
        /// Gets the root node.
        /// </summary>
        internal Node Root => _root;

        #region Tree Structure

        /// <summary>
        /// Represents a simple node.
        /// </summary>
        internal class Node
        {
            /// <summary>
            /// Gets or sets boxes.
            /// </summary>
			public List<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)> Boxes { get; set; }

            /// <summary>
            /// Gets or sets the children.
            /// </summary>
            public IList Children { get; set; }

            /// <summary>
            /// Gets or sets the parent.
            /// </summary>
            public Node Parent { get; set; }

            /// <summary>
            /// Returns the bounding box for this node.
            /// </summary>
            /// <returns></returns>
			public ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) GetBox()
            {
				var box = this.Boxes[0];
                for (var idx = 1; idx < this.Boxes.Count; idx++)
                {
                    box = box.Expand(this.Boxes[idx]);
                }
                return box;
            }
        }

        #region Tree Operations

        /// <summary>
        /// Fills the collection with data.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="box"></param>
        /// <param name="result"></param>
        private static void Get(Node node, ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box, HashSet<T> result)
        {
            if (node.Children is List<Node> childNodes)
            {
                for (var idx = 0; idx < childNodes.Count; idx++)
                {
                    if (!box.Overlaps(node.Boxes[idx])) continue;
                    
                    if (box.Covers(node.Boxes[idx]))
                    { // add all the data from the child.
                        RTree<T>.GetAll(childNodes[idx],
                            result);
                    }
                    else
                    { // add the data from the child.
                        RTree<T>.Get(childNodes[idx],
                            box, result);
                    }
                }
            }
            else
            {
                if (!(node.Children is List<T> children)) return;
                
                // the children are of the data type.
                for (var idx = 0; idx < node.Children.Count; idx++)
                {
                    if (node.Boxes[idx].Overlaps(box))
                    {
                        result.Add(children[idx]);
                    }
                }
            }
        }

        private static void GetAll(Node node, HashSet<T> result)
        {
            if (node.Children is List<Node>)
            {
                var children = (node.Children as List<Node>);
                for (int idx = 0; idx < children.Count; idx++)
                {
                    // add all the data from the child.
                    RTree<T>.GetAll(children[idx],
                                                     result);
                }
            }
            else
            {
                var children = (node.Children as List<T>);
                if (children != null)
                { // the children are of the data type.
                    for (int idx = 0; idx < node.Children.Count; idx++)
                    {
                        result.Add(children[idx]);
                    }
                }
            }
        }

		private static Node Add(Node leaf, ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box, T item, int minimumSize, int maximumSize)
        {
            if (leaf == null) throw new ArgumentNullException(nameof(leaf));

            Node ll = null;
            if (leaf.Boxes.Count == maximumSize)
            { // split the node.
                // add the child.
                leaf.Boxes.Add(box);
                leaf.Children.Add(item);

                Node[] split = RTree<T>.SplitNode(leaf, minimumSize);
                leaf.Boxes = split[0].Boxes;
                leaf.Children = split[0].Children;
                RTree<T>.SetParents(leaf);
                ll = split[1];
            }
            else
            {
                // add the child.
                leaf.Boxes.Add(box);
                leaf.Children.Add(item);
            }

            // adjust the tree.
            Node n = leaf;
            Node nn = ll;
            while (n.Parent != null)
            { // keep going until the root is reached.
                Node p = n.Parent;
                RTree<T>.TightenFor(p, n); // tighten the parent box around n.

                if (nn != null)
                { // propagate split if needed.
                    if (p.Boxes.Count == maximumSize)
                    { // parent needs to be split.
                        p.Boxes.Add(nn.GetBox());
                        p.Children.Add(nn);
                        Node[] split = RTree<T>.SplitNode(
                            p, minimumSize);
                        p.Boxes = split[0].Boxes;
                        p.Children = split[0].Children;
                        RTree<T>.SetParents(p);
                        nn = split[1];
                    }
                    else
                    { // add the other 'split' node.
                        p.Boxes.Add(nn.GetBox());
                        p.Children.Add(nn);
                        nn.Parent = p;
                        nn = null;
                    }
                }
                n = p;
            }
            if (nn != null)
            { // create a new root node and 
                var root = new Node();
				root.Boxes = new List<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)>();
                root.Boxes.Add(n.GetBox());
                root.Boxes.Add(nn.GetBox());
                root.Children = new List<Node>();
                root.Children.Add(n);
                n.Parent = root;
                root.Children.Add(nn);
                nn.Parent = root;
                return root;
            }
            return null; // no new root node needed.
        }

        /// <summary>
        /// Removes the given item but does not re-balance the tree.
        /// </summary>
        /// <param name="node">The node to begin the search for the item.</param>
        /// <param name="box">The box of the item.</param>
        /// <param name="item">The item to remove.</param>
        private static bool RemoveSimple(Node node, ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box, T item)
        {
            if (node.Children is List<Node> childNodes)
            {
                for (var idx = 0; idx < childNodes.Count; idx++)
                {
                    if (!box.Overlaps(node.Boxes[idx])) continue;
                    
                    if(RTree<T>.RemoveSimple(childNodes[idx], box, item))
                    { // if successful stop the search.
                        return true;
                    }
                }
            }
            else
            {
                // this is the leaf node we are looking for.
                var index = node.Children.IndexOf(item);
                if (index < 0) return false;
                
                // this is the leaf node we are looking for.
                node.Children.RemoveAt(index);
                node.Boxes.RemoveAt(index);
                return true;
            }

            return false;
        }

        private static void TightenFor(Node parent, Node child)
        {
            for (int idx = 0; idx < parent.Children.Count; idx++)
            {
                if (parent.Children[idx] == child)
                {
                    parent.Boxes[idx] = child.GetBox();
                }
            }
        }
        
		private static Node ChooseLeaf(Node node, ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            // keep looping until a leaf is found.
            while (node.Children is List<Node>)
            { // choose the best leaf.
                Node bestChild = null;
                ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)? bestBox = null;
                double bestIncrease = double.MaxValue;
                var children = node.Children as List<Node>; // cast just once.
                for (int idx = 0; idx < node.Boxes.Count; idx++)
                {
                    ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) union = node.Boxes[idx].Expand(box);
                    double increase = union.Surface() - node.Boxes[idx].Surface(); // calculates the increase.
                    if (bestIncrease > increase)
                    {
                        // the increase for this child is smaller.
                        bestIncrease = increase;
                        bestChild = children[idx];
                        bestBox = node.Boxes[idx];
                    }
                    else if (bestBox != null &&
                             bestIncrease == increase)
                    {
                        // the increase is identical, choose the smallest child.
                        if (node.Boxes[idx].Surface() < bestBox.Value.Surface())
                        {
                            bestChild = children[idx];
                            bestBox = node.Boxes[idx];
                        }
                    }
                }

                node = bestChild ?? throw new Exception("Finding best child failed!");
            }
            return node;
        }

        /// <summary>
        /// Sets all the parent properties of the children of the given node.
        /// </summary>
        /// <param name="node"></param>
        private static void SetParents(Node node)
        {
            if (!(node.Children is List<Node> children)) return;
            
            for (var idx = 0; idx < node.Boxes.Count; idx++)
            {
                children[idx].Parent = node;
            }
        }

        /// <summary>
        /// Splits the given node in two other nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="minimumSize"></param>
        /// <returns></returns>
        private static Node[] SplitNode(Node node, int minimumSize)
        {
            bool leaf = (node.Children is List<T>);

            // create the target nodes.
            var nodes = new Node[2];
            nodes[0] = new Node();
			nodes[0].Boxes = new List<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)>();
            if (leaf)
            {
                nodes[0].Children = new List<T>();
            }
            else
            {
                nodes[0].Children = new List<Node>();
            }
            nodes[1] = new Node();
			nodes[1].Boxes = new List<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)>();
            if (leaf)
            {
                nodes[1].Children = new List<T>();
            }
            else
            {
                nodes[1].Children = new List<Node>();
            }

            // select the seed boxes.
            var seeds = RTree<T>.SelectSeeds(node.Boxes);

            // add the boxes.
            nodes[0].Boxes.Add(node.Boxes[seeds[0]]);
            nodes[1].Boxes.Add(node.Boxes[seeds[1]]);
            nodes[0].Children.Add(node.Children[seeds[0]]);
            nodes[1].Children.Add(node.Children[seeds[1]]);

            // create the boxes.
			var boxes = new ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)[2]
                            {
                                node.Boxes[seeds[0]], node.Boxes[seeds[1]]
                            };
            node.Boxes.RemoveAt(seeds[0]); // seeds[1] is always < seeds[0].
            node.Boxes.RemoveAt(seeds[1]);
            node.Children.RemoveAt(seeds[0]);
            node.Children.RemoveAt(seeds[1]);

            while (node.Boxes.Count > 0)
            {
                // check if one of them needs em all!
                if (nodes[0].Boxes.Count + node.Boxes.Count == minimumSize)
                { // all remaining boxes need te be assigned here.
                    for (int idx = 0; node.Boxes.Count > 0; idx++)
                    {
                        boxes[0] = boxes[0].Expand(node.Boxes[0]);
                        nodes[0].Boxes.Add(node.Boxes[0]);
                        nodes[0].Children.Add(node.Children[0]);

                        node.Boxes.RemoveAt(0);
                        node.Children.RemoveAt(0);
                    }
                }
                else if (nodes[1].Boxes.Count + node.Boxes.Count == minimumSize)
                { // all remaining boxes need te be assigned here.
                    for (int idx = 0; node.Boxes.Count > 0; idx++)
                    {
                        boxes[1] = boxes[1].Expand(node.Boxes[0]);
                        nodes[1].Boxes.Add(node.Boxes[0]);
                        nodes[1].Children.Add(node.Children[0]);

                        node.Boxes.RemoveAt(0);
                        node.Children.RemoveAt(0);
                    }
                }
                else
                { // choose one of the leaves.
                    var nextId = RTree<T>.PickNext(boxes, node.Boxes, out var leafIdx);

                    boxes[leafIdx] = boxes[leafIdx].Expand(node.Boxes[nextId]);

                    nodes[leafIdx].Boxes.Add(node.Boxes[nextId]);
                    nodes[leafIdx].Children.Add(node.Children[nextId]);

                    node.Boxes.RemoveAt(nextId);
                    node.Children.RemoveAt(nextId);
                }
            }

            RTree<T>.SetParents(nodes[0]);
            RTree<T>.SetParents(nodes[1]);

            return nodes;
        }
        
		private static int PickNext(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)[] nodeBoxes, 
            IList<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)> boxes, out int nodeBoxIndex)
        {
            var difference = double.MinValue;
            nodeBoxIndex = 0;
            var pickedIdx = -1;
            for (var idx = 0; idx < boxes.Count; idx++)
            {
				var item = boxes[idx];
                var d1 = item.Expand(nodeBoxes[0]).Surface() -
                         item.Surface();
                var d2 = item.Expand(nodeBoxes[1]).Surface() -
                         item.Surface();

                var localDifference = System.Math.Abs(d1 - d2);
                if (!(difference < localDifference)) continue;
                difference = localDifference;
                if (d1 == d2)
                {
                    nodeBoxIndex = (nodeBoxes[0].Surface() < nodeBoxes[1].Surface()) ? 0 : 1;
                }
                else
                {
                    nodeBoxIndex = (d1 < d2) ? 0 : 1;
                }
                pickedIdx = idx;
            }
            return pickedIdx;
        }
        
		private static int[] SelectSeeds(List<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)> boxes)
        {
            if (boxes == null) throw new ArgumentNullException(nameof(boxes));
            if (boxes.Count < 2) throw new ArgumentException("Cannot select seeds from a list with less than two items.");

            // the Quadratic Split version: selecting the two items that waste the most space
            // if put together.

            var seeds = new int[2];
            var loss = double.MinValue;
            for (var idx1 = 0; idx1 < boxes.Count; idx1++)
            {
                for (var idx2 = 0; idx2 < idx1; idx2++)
                {
                    var localLoss = System.Math.Max(boxes[idx1].Expand(boxes[idx2]).Surface() -
                                                    boxes[idx1].Surface() - boxes[idx2].Surface(), 0);
                    if (!(localLoss > loss)) continue;
                    
                    loss = localLoss;
                    seeds[0] = idx1;
                    seeds[1] = idx2;
                }
            }

            return seeds;
        }

        #endregion

        #endregion

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new RTreeEnumerator(_root);
        }

        /// <summary>
        /// Returns a enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new RTreeEnumerator(_root);
        }

        /// <summary>
        /// Enumerates everything in this index.
        /// </summary>
        internal class RTreeEnumerator : IEnumerator<T>
        {
            /// <summary>
            /// Holds the root node.
            /// </summary>
            private Node _root;

            /// <summary>
            /// Holds the current position.
            /// </summary>
            private NodePosition _current;

            /// <summary>
            /// Creates a new enumerator.
            /// </summary>
            /// <param name="root"></param>
            public RTreeEnumerator(Node root)
            {
                _root = root;
            }

            /// <summary>
            /// Returns the current node.
            /// </summary>
            public T Current => (T)_current.Node.Children[_current.NodeIdx];

            /// <summary>
            /// Diposes all resource associtated with this enumerator.
            /// </summary>
            public void Dispose()
            {
                _root = null;
                _current = null;
            }

            /// <summary>
            /// Returns the current object.
            /// </summary>
            object IEnumerator.Current => this.Current;

            /// <summary>
            /// Move next.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                NodePosition position = null;
                _current ??= new NodePosition() {Node = _root, Parent = null, NodeIdx = -1};
                position = MoveNextFrom(_current);

                _current = position;
                return _current != null;
            }

            /// <summary>
            /// Move to the next position from the given position.
            /// </summary>
            /// <param name="position"></param>
            /// <returns></returns>
            private static NodePosition MoveNextFrom(NodePosition position)
            {
                position.NodeIdx++; // move to the next position.
                while (position.Node.Children == null ||
                    position.Node.Children.Count <= position.NodeIdx ||
                    position.Node.Children[position.NodeIdx] is Node)
                { // there is a need to move to the next object because the current one does not exist.
                    if (position.Node.Children != null && 
                        position.Node.Children.Count > position.NodeIdx &&
                        position.Node.Children[position.NodeIdx] is Node)
                    { // the current child is not of type T move to the child of the child.
                        NodePosition nextPosition = new NodePosition();
                        nextPosition.Parent = position;
                        nextPosition.NodeIdx = -1;
                        nextPosition.Node = position.Node.Children[position.NodeIdx] as Node;

                        position = nextPosition;
                    }
                    else
                    { // there are no children or the children are finished, move to the parent.
                        position = position.Parent;
                    }

                    if (position == null)
                    { // the position is null, no more next position.
                        break;
                    }
                    position.NodeIdx++; // move to the next position.
                }
                return position;
            }

            /// <summary>
            /// Reset this enumerator.
            /// </summary>
            public void Reset()
            {
                _current = null;
            }

            private class NodePosition
            {
                /// <summary>
                /// Gets/sets the parent.
                /// </summary>
                public NodePosition Parent { get; set; }

                /// <summary>
                /// Gets/sets the node.
                /// </summary>
                public Node Node { get; set; }

                /// <summary>
                /// Gets/sets the current node index.
                /// </summary>
                public int NodeIdx { get; set; }
            }
        }
    }
}