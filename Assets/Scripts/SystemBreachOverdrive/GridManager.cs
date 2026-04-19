using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SystemBreachOverdrive
{
    public class GridManager : MonoBehaviour
    {
        private const float CellSpacing = 1.05f;

        private readonly List<GridNode> _allNodes = new List<GridNode>();
        private readonly List<Vector2Int> _pathBuffer = new List<Vector2Int>();
        private readonly List<GridNode> _exitNodes = new List<GridNode>();
        private readonly List<GridNode> _requiredNodes = new List<GridNode>();
        private readonly List<GridNode> _overloadedNodes = new List<GridNode>();

        private GridNode[,] _nodes;
        private Transform _gridRoot;
        private System.Random _random;
        private int _gridSize;
        private bool _interactionEnabled;

        public event Action OnGridChanged;
        public event Action<GridNode> OnNodeRotated;

        public int GridSize => _gridSize;
        public LevelConfig CurrentConfig { get; private set; }
        public GridNode SourceNode { get; private set; }
        public IReadOnlyList<GridNode> ExitNodes => _exitNodes;
        public IReadOnlyList<GridNode> RequiredNodes => _requiredNodes;

        private void Update()
        {
            if (!_interactionEnabled)
            {
                return;
            }

            if (!IsPrimaryClickPressedThisFrame())
            {
                return;
            }

            var cameraRef = Camera.main;
            if (cameraRef == null)
            {
                return;
            }

            var pointerPosition = GetPointerScreenPosition();
            var world = cameraRef.ScreenToWorldPoint(pointerPosition);
            var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            if (hit == null)
            {
                return;
            }

            var node = hit.GetComponent<GridNode>();
            if (node != null)
            {
                OnNodeClicked(node);
            }
        }

        public void Generate(LevelConfig config)
        {
            CurrentConfig = config;
            _gridSize = Mathf.Max(2, config.GridSize);
            _random = new System.Random(config.Seed);
            _interactionEnabled = true;

            EnsureRoot();
            ClearGrid();

            var blueprint = BuildBlueprint(config);
            SpawnNodes(blueprint);
            RefreshSignalState();
            OnGridChanged?.Invoke();
        }

        public void OnNodeClicked(GridNode node)
        {
            if (!_interactionEnabled)
            {
                return;
            }

            if (node == null)
            {
                return;
            }

            if (!node.TryRotateClockwise())
            {
                return;
            }

            OnNodeRotated?.Invoke(node);
            RefreshSignalState();
            OnGridChanged?.Invoke();
        }

        public bool IsExitConnected()
        {
            return IsLevelCompleted();
        }

        public bool IsLevelCompleted()
        {
            if (SourceNode == null || _exitNodes.Count == 0)
            {
                return false;
            }

            var connected = CollectConnectedNodesFromSource();
            for (var i = 0; i < _exitNodes.Count; i++)
            {
                if (!connected.Contains(_exitNodes[i]))
                {
                    return false;
                }
            }

            for (var i = 0; i < _requiredNodes.Count; i++)
            {
                if (!connected.Contains(_requiredNodes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public void GetObjectiveProgress(out int connectedObjectives, out int totalObjectives)
        {
            var objectiveSet = new HashSet<GridNode>();
            for (var i = 0; i < _exitNodes.Count; i++)
            {
                objectiveSet.Add(_exitNodes[i]);
            }

            for (var i = 0; i < _requiredNodes.Count; i++)
            {
                objectiveSet.Add(_requiredNodes[i]);
            }

            totalObjectives = objectiveSet.Count;
            if (totalObjectives == 0)
            {
                connectedObjectives = 0;
                return;
            }

            var connected = CollectConnectedNodesFromSource();
            connectedObjectives = 0;
            foreach (var objective in objectiveSet)
            {
                if (connected.Contains(objective))
                {
                    connectedObjectives++;
                }
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
        }

        public bool TryGetSignalPath(out List<GridNode> orderedPath)
        {
            orderedPath = new List<GridNode>();

            if (SourceNode == null || _exitNodes.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < _exitNodes.Count; i++)
            {
                if (TryGetPathToTarget(_exitNodes[i], out orderedPath))
                {
                    return true;
                }
            }

            orderedPath.Clear();
            return false;
        }

        public bool TryGetAllExitPaths(out List<List<GridNode>> paths)
        {
            paths = new List<List<GridNode>>();
            if (SourceNode == null || _exitNodes.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < _exitNodes.Count; i++)
            {
                if (!TryGetPathToTarget(_exitNodes[i], out var path))
                {
                    return false;
                }

                paths.Add(path);
            }

            return paths.Count > 0;
        }

        private bool TryGetPathToTarget(GridNode target, out List<GridNode> orderedPath)
        {
            orderedPath = new List<GridNode>();

            if (target == null)
            {
                return false;
            }

            var queue = new Queue<GridNode>();
            var visited = new HashSet<GridNode>();
            var parentMap = new Dictionary<GridNode, GridNode>();

            queue.Enqueue(SourceNode);
            visited.Add(SourceNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    break;
                }

                for (var i = 0; i < 4; i++)
                {
                    var direction = (Direction)i;
                    if (!current.HasConnection(direction))
                    {
                        continue;
                    }

                    var nextPosition = current.GridPosition + direction.ToVector2Int();
                    if (!IsInside(nextPosition))
                    {
                        continue;
                    }

                    var neighbor = _nodes[nextPosition.x, nextPosition.y];
                    if (neighbor == null || !neighbor.HasConnection(direction.Opposite()) || !visited.Add(neighbor))
                    {
                        continue;
                    }

                    parentMap[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            if (!visited.Contains(target))
            {
                return false;
            }

            var cursor = target;
            orderedPath.Add(cursor);

            while (cursor != SourceNode)
            {
                if (!parentMap.TryGetValue(cursor, out var parent))
                {
                    orderedPath.Clear();
                    return false;
                }

                cursor = parent;
                orderedPath.Add(cursor);
            }

            orderedPath.Reverse();
            return true;
        }

        public void RefreshSignalState()
        {
            var connected = CollectConnectedNodesFromSource();

            for (var i = 0; i < _allNodes.Count; i++)
            {
                _allNodes[i].SetActive(connected.Contains(_allNodes[i]));
            }
        }

        private void EnsureRoot()
        {
            if (_gridRoot != null)
            {
                return;
            }

            var root = new GameObject("GridRoot");
            root.transform.SetParent(transform, false);
            _gridRoot = root.transform;
        }

        private void ClearGrid()
        {
            if (_gridRoot == null)
            {
                return;
            }

            for (var i = _gridRoot.childCount - 1; i >= 0; i--)
            {
                var child = _gridRoot.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            _allNodes.Clear();
            _exitNodes.Clear();
            _requiredNodes.Clear();
            _overloadedNodes.Clear();
            _nodes = new GridNode[_gridSize, _gridSize];
            SourceNode = null;
        }

        private NodeBlueprint[,] BuildBlueprint(LevelConfig config)
        {
            var map = new NodeBlueprint[_gridSize, _gridSize];
            var connections = new bool[_gridSize, _gridSize, 4];
            var mainPathSet = new HashSet<Vector2Int>();

            _pathBuffer.Clear();
            BuildMainPath(_pathBuffer);
            for (var i = 0; i < _pathBuffer.Count; i++)
            {
                mainPathSet.Add(_pathBuffer[i]);
            }

            for (var i = 0; i < _pathBuffer.Count - 1; i++)
            {
                var current = _pathBuffer[i];
                var next = _pathBuffer[i + 1];
                var direction = GetDirection(current, next);

                connections[current.x, current.y, (int)direction] = true;
                connections[next.x, next.y, (int)direction.Opposite()] = true;
            }

            if (config.AllowTNodes)
            {
                AddOptionalBranches(connections);
            }

            for (var x = 0; x < _gridSize; x++)
            {
                for (var y = 0; y < _gridSize; y++)
                {
                    var openings = ReadConnections(connections, x, y);
                    var onPath = mainPathSet.Contains(new Vector2Int(x, y));

                    if (!onPath)
                    {
                        var randomType = GetRandomNodeType(config.AllowTNodes);
                        openings = BuildOpeningsForType(randomType);
                    }

                    map[x, y] = new NodeBlueprint
                    {
                        BaseConnections = openings,
                        RotationSteps = 0,
                        IsLocked = false,
                        Type = GuessType(openings)
                    };
                }
            }

            var sourcePosition = _pathBuffer[0];
            var exitPositions = SelectExitPositions(sourcePosition, Mathf.Max(1, config.ExitCount));
            var requiredPositions = SelectRequiredPositions(sourcePosition, exitPositions, config.RequiredNodes);
            var overloadedPositions = SelectOverloadedPositions(sourcePosition, exitPositions, config.OverloadedNodes);

            map[sourcePosition.x, sourcePosition.y].IsSource = true;
            map[sourcePosition.x, sourcePosition.y].IsLocked = true;
            map[sourcePosition.x, sourcePosition.y].Type = NodeType.Locked;

            foreach (var exitPosition in exitPositions)
            {
                map[exitPosition.x, exitPosition.y].IsExit = true;
                map[exitPosition.x, exitPosition.y].IsLocked = true;
                map[exitPosition.x, exitPosition.y].Type = NodeType.Locked;
            }

            foreach (var requiredPosition in requiredPositions)
            {
                map[requiredPosition.x, requiredPosition.y].IsRequired = true;
            }

            foreach (var overloadedPosition in overloadedPositions)
            {
                map[overloadedPosition.x, overloadedPosition.y].IsOverloaded = true;
            }

            ApplyLockedNodes(map, config.LockedNodes, sourcePosition, exitPositions);
            ApplyRandomRotations(map);

            return map;
        }

        private void SpawnNodes(NodeBlueprint[,] blueprint)
        {
            var offset = (_gridSize - 1) * CellSpacing * 0.5f;

            for (var y = 0; y < _gridSize; y++)
            {
                for (var x = 0; x < _gridSize; x++)
                {
                    var nodeObject = new GameObject($"Node_{x}_{y}");
                    nodeObject.transform.SetParent(_gridRoot, false);
                    nodeObject.transform.localPosition = new Vector3(x * CellSpacing - offset, y * CellSpacing - offset, 0f);

                    var node = nodeObject.AddComponent<GridNode>();
                    var blueprintNode = blueprint[x, y];

                    node.Initialize(
                        this,
                        new Vector2Int(x, y),
                        blueprintNode.BaseConnections,
                        blueprintNode.RotationSteps,
                        blueprintNode.IsLocked,
                        blueprintNode.IsSource,
                        blueprintNode.IsExit,
                        blueprintNode.Type,
                        blueprintNode.IsRequired,
                        blueprintNode.IsOverloaded);

                    _nodes[x, y] = node;
                    _allNodes.Add(node);

                    if (blueprintNode.IsSource)
                    {
                        SourceNode = node;
                    }

                    if (blueprintNode.IsExit)
                    {
                        _exitNodes.Add(node);
                    }

                    if (blueprintNode.IsRequired)
                    {
                        _requiredNodes.Add(node);
                    }

                    if (blueprintNode.IsOverloaded)
                    {
                        _overloadedNodes.Add(node);
                    }
                }
            }
        }

        private HashSet<GridNode> CollectConnectedNodesFromSource()
        {
            var connected = new HashSet<GridNode>();

            if (SourceNode == null)
            {
                return connected;
            }

            var queue = new Queue<GridNode>();
            queue.Enqueue(SourceNode);
            connected.Add(SourceNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                for (var i = 0; i < 4; i++)
                {
                    var direction = (Direction)i;
                    if (!current.HasConnection(direction))
                    {
                        continue;
                    }

                    var nextPosition = current.GridPosition + direction.ToVector2Int();
                    if (!IsInside(nextPosition))
                    {
                        continue;
                    }

                    var neighbor = _nodes[nextPosition.x, nextPosition.y];
                    if (neighbor == null || !neighbor.HasConnection(direction.Opposite()))
                    {
                        continue;
                    }

                    if (connected.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return connected;
        }

        private bool[] ReadConnections(bool[,,] connections, int x, int y)
        {
            var openings = new bool[4];
            for (var i = 0; i < 4; i++)
            {
                openings[i] = connections[x, y, i];
            }

            return openings;
        }

        private void BuildMainPath(List<Vector2Int> path)
        {
            var position = Vector2Int.zero;
            path.Add(position);

            var safetyGuard = _gridSize * _gridSize * 4;
            var steps = 0;

            while (position.x < _gridSize - 1 || position.y < _gridSize - 1)
            {
                steps++;
                if (steps > safetyGuard)
                {
                    break;
                }

                var canGoRight = position.x < _gridSize - 1;
                var canGoDown = position.y < _gridSize - 1;

                if (canGoRight && canGoDown)
                {
                    position += _random.NextDouble() > 0.5 ? Vector2Int.right : Vector2Int.up;
                }
                else if (canGoRight)
                {
                    position += Vector2Int.right;
                }
                else
                {
                    position += Vector2Int.up;
                }

                path.Add(position);
            }

            var target = new Vector2Int(_gridSize - 1, _gridSize - 1);
            if (path[path.Count - 1] != target)
            {
                path.Clear();
                position = Vector2Int.zero;
                path.Add(position);

                while (position.x < target.x)
                {
                    position += Vector2Int.right;
                    path.Add(position);
                }

                while (position.y < target.y)
                {
                    position += Vector2Int.up;
                    path.Add(position);
                }
            }
        }

        private void AddOptionalBranches(bool[,,] connections)
        {
            for (var i = 1; i < _pathBuffer.Count - 1; i++)
            {
                if (_random.NextDouble() > 0.35)
                {
                    continue;
                }

                var current = _pathBuffer[i];
                var tryDirections = new List<Direction> { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
                Shuffle(tryDirections);

                for (var d = 0; d < tryDirections.Count; d++)
                {
                    var direction = tryDirections[d];
                    var neighbor = current + direction.ToVector2Int();
                    if (!IsInside(neighbor))
                    {
                        continue;
                    }

                    connections[current.x, current.y, (int)direction] = true;
                    connections[neighbor.x, neighbor.y, (int)direction.Opposite()] = true;
                    break;
                }
            }
        }

        private void ApplyLockedNodes(NodeBlueprint[,] map, int lockedNodes, Vector2Int sourcePos, HashSet<Vector2Int> exitPositions)
        {
            if (lockedNodes <= 0)
            {
                return;
            }

            var candidates = new List<Vector2Int>();
            for (var x = 0; x < _gridSize; x++)
            {
                for (var y = 0; y < _gridSize; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (position == sourcePos || exitPositions.Contains(position))
                    {
                        continue;
                    }

                    candidates.Add(position);
                }
            }

            Shuffle(candidates);
            var lockCount = Mathf.Min(lockedNodes, candidates.Count);

            for (var i = 0; i < lockCount; i++)
            {
                var p = candidates[i];
                map[p.x, p.y].IsLocked = true;
                map[p.x, p.y].Type = NodeType.Locked;
            }
        }

        private void ApplyRandomRotations(NodeBlueprint[,] map)
        {
            for (var x = 0; x < _gridSize; x++)
            {
                for (var y = 0; y < _gridSize; y++)
                {
                    if (map[x, y].IsLocked)
                    {
                        continue;
                    }

                    map[x, y].RotationSteps = _random.Next(0, 4);
                }
            }
        }

        private static Direction GetDirection(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;

            if (delta == Vector2Int.up)
            {
                return Direction.Up;
            }

            if (delta == Vector2Int.right)
            {
                return Direction.Right;
            }

            if (delta == Vector2Int.down)
            {
                return Direction.Down;
            }

            return Direction.Left;
        }

        private bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < _gridSize && position.y >= 0 && position.y < _gridSize;
        }

        private NodeType GetRandomNodeType(bool allowT)
        {
            if (!allowT)
            {
                return _random.NextDouble() > 0.5 ? NodeType.Straight : NodeType.Corner;
            }

            var roll = _random.NextDouble();
            if (roll < 0.34)
            {
                return NodeType.Straight;
            }

            if (roll < 0.68)
            {
                return NodeType.Corner;
            }

            return NodeType.T;
        }

        private static NodeType GuessType(bool[] openings)
        {
            var count = 0;
            for (var i = 0; i < openings.Length; i++)
            {
                if (openings[i])
                {
                    count++;
                }
            }

            if (count == 3)
            {
                return NodeType.T;
            }

            if (count == 4)
            {
                return NodeType.Cross;
            }

            if (count == 2)
            {
                var oppositePair = (openings[(int)Direction.Up] && openings[(int)Direction.Down]) ||
                                   (openings[(int)Direction.Left] && openings[(int)Direction.Right]);
                return oppositePair ? NodeType.Straight : NodeType.Corner;
            }

            return NodeType.Straight;
        }

        private static bool[] BuildOpeningsForType(NodeType type)
        {
            var openings = new bool[4];

            switch (type)
            {
                case NodeType.Straight:
                    openings[(int)Direction.Up] = true;
                    openings[(int)Direction.Down] = true;
                    break;
                case NodeType.Corner:
                    openings[(int)Direction.Up] = true;
                    openings[(int)Direction.Right] = true;
                    break;
                case NodeType.T:
                    openings[(int)Direction.Up] = true;
                    openings[(int)Direction.Left] = true;
                    openings[(int)Direction.Right] = true;
                    break;
                case NodeType.Cross:
                    openings[(int)Direction.Up] = true;
                    openings[(int)Direction.Right] = true;
                    openings[(int)Direction.Down] = true;
                    openings[(int)Direction.Left] = true;
                    break;
                default:
                    openings[(int)Direction.Up] = true;
                    openings[(int)Direction.Down] = true;
                    break;
            }

            return openings;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var randomIndex = _random.Next(0, i + 1);
                var temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        private struct NodeBlueprint
        {
            public bool[] BaseConnections;
            public int RotationSteps;
            public bool IsLocked;
            public bool IsSource;
            public bool IsExit;
            public bool IsRequired;
            public bool IsOverloaded;
            public NodeType Type;
        }

        private HashSet<Vector2Int> SelectExitPositions(Vector2Int sourcePosition, int requestedExitCount)
        {
            var candidates = new List<Vector2Int>(_pathBuffer);
            candidates.Remove(sourcePosition);

            var result = new HashSet<Vector2Int>();
            if (candidates.Count == 0)
            {
                return result;
            }

            var exitCount = Mathf.Clamp(requestedExitCount, 1, candidates.Count);
            var step = Mathf.Max(1, candidates.Count / exitCount);

            for (var i = 0; i < exitCount; i++)
            {
                var idx = Mathf.Clamp(candidates.Count - 1 - (i * step), 0, candidates.Count - 1);
                result.Add(candidates[idx]);
            }

            var fallbackIndex = 0;
            while (result.Count < exitCount && fallbackIndex < candidates.Count)
            {
                result.Add(candidates[fallbackIndex]);
                fallbackIndex++;
            }

            return result;
        }

        private HashSet<Vector2Int> SelectRequiredPositions(Vector2Int sourcePosition, HashSet<Vector2Int> exits, int requestedRequired)
        {
            var candidates = new List<Vector2Int>();
            for (var i = 0; i < _pathBuffer.Count; i++)
            {
                var position = _pathBuffer[i];
                if (position == sourcePosition || exits.Contains(position))
                {
                    continue;
                }

                candidates.Add(position);
            }

            Shuffle(candidates);
            var count = Mathf.Clamp(requestedRequired, 0, candidates.Count);
            var result = new HashSet<Vector2Int>();
            for (var i = 0; i < count; i++)
            {
                result.Add(candidates[i]);
            }

            return result;
        }

        private HashSet<Vector2Int> SelectOverloadedPositions(Vector2Int sourcePosition, HashSet<Vector2Int> exits, int requestedOverloaded)
        {
            var candidates = new List<Vector2Int>();
            for (var x = 0; x < _gridSize; x++)
            {
                for (var y = 0; y < _gridSize; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (position == sourcePosition || exits.Contains(position))
                    {
                        continue;
                    }

                    candidates.Add(position);
                }
            }

            Shuffle(candidates);
            var count = Mathf.Clamp(requestedOverloaded, 0, candidates.Count);
            var result = new HashSet<Vector2Int>();
            for (var i = 0; i < count; i++)
            {
                result.Add(candidates[i]);
            }

            return result;
        }

        private static bool IsPrimaryClickPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static Vector3 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }
}
