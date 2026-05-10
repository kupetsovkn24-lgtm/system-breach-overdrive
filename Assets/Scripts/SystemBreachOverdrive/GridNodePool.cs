using System.Collections.Generic;
using UnityEngine;

namespace SystemBreachOverdrive
{
    public sealed class GridNodePool
    {
        private readonly Stack<GridNode> _availableNodes = new Stack<GridNode>();
        private readonly Transform _parent;
        private int _createdCount;

        public int ActiveCount => Mathf.Max(0, _createdCount - _availableNodes.Count);
        public int CachedCount => _availableNodes.Count;
        public int CreatedCount => _createdCount;
        public int ReusedCount { get; private set; }

        public GridNodePool(Transform parent)
        {
            _parent = parent;
        }

        public GridNode Get(string objectName, Vector3 localPosition)
        {
            GridNode node;
            if (_availableNodes.Count > 0)
            {
                node = _availableNodes.Pop();
                ReusedCount++;
            }
            else
            {
                node = CreateNode();
            }

            var nodeTransform = node.transform;
            nodeTransform.SetParent(_parent, false);
            nodeTransform.localPosition = localPosition;
            nodeTransform.localRotation = Quaternion.identity;
            nodeTransform.localScale = Vector3.one;

            node.gameObject.name = objectName;
            node.gameObject.SetActive(true);
            return node;
        }

        public void Release(GridNode node)
        {
            if (node == null)
            {
                return;
            }

            node.ResetForPool();
            node.transform.SetParent(_parent, false);
            node.gameObject.SetActive(false);
            _availableNodes.Push(node);
        }

        private GridNode CreateNode()
        {
            var nodeObject = new GameObject("GridNode_Pooled");
            nodeObject.SetActive(false);
            nodeObject.transform.SetParent(_parent, false);

            var node = nodeObject.AddComponent<GridNode>();
            _createdCount++;
            return node;
        }
    }
}
