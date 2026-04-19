using System.Collections;
using UnityEngine;

namespace SystemBreachOverdrive
{
    public enum NodeType
    {
        Straight,
        Corner,
        T,
        Cross,
        Locked
    }

    [RequireComponent(typeof(BoxCollider2D))]
    public class GridNode : MonoBehaviour
    {
        private static readonly Color BodyColor = new Color(0.07f, 0.10f, 0.14f);
        private static readonly Color BodyLockedColor = new Color(0.16f, 0.16f, 0.18f);
        private static readonly Color SourceColor = new Color(0.14f, 0.42f, 0.24f);
        private static readonly Color ExitColor = new Color(0.42f, 0.16f, 0.46f);
        private static readonly Color SourceHighlightColor = new Color(0.22f, 0.92f, 0.52f, 0.55f);
        private static readonly Color ExitHighlightColor = new Color(0.92f, 0.32f, 0.98f, 0.55f);
        private static readonly Color ConnectorColor = new Color(0.20f, 0.75f, 0.95f);
        private static readonly Color ActiveColor = new Color(0.36f, 0.96f, 1.00f);

        private readonly SpriteRenderer[] _connectors = new SpriteRenderer[4];

        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _pipeRenderer;
        private SpriteRenderer _endpointHighlightRenderer;
        private bool[] _baseConnections = new bool[4];
        private bool _isActive;
        private bool _isPulsing;
        private Coroutine _pulseRoutine;
        private bool _isRequired;
        private bool _isOverloaded;

        public GridManager Owner { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public int RotationSteps { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsSource { get; private set; }
        public bool IsExit { get; private set; }
        public NodeType Type { get; private set; }
        public bool IsRequired => _isRequired;
        public bool IsOverloaded => _isOverloaded;

        public void Initialize(
            GridManager owner,
            Vector2Int gridPosition,
            bool[] baseConnections,
            int rotationSteps,
            bool isLocked,
            bool isSource,
            bool isExit,
            NodeType type,
            bool isRequired,
            bool isOverloaded)
        {
            Owner = owner;
            GridPosition = gridPosition;
            _baseConnections = new bool[4];

            for (var i = 0; i < 4; i++)
            {
                _baseConnections[i] = i < baseConnections.Length && baseConnections[i];
            }

            RotationSteps = ((rotationSteps % 4) + 4) % 4;
            IsSource = isSource;
            IsExit = isExit;
            IsLocked = isLocked || isSource || isExit;
            Type = type;
            _isRequired = isRequired;
            _isOverloaded = isOverloaded;

            EnsureVisuals();
            RefreshVisual();
        }

        private void OnEnable()
        {
            GamePalette.OnPaletteChanged += HandlePaletteChanged;
        }

        private void OnDisable()
        {
            GamePalette.OnPaletteChanged -= HandlePaletteChanged;
        }

        private void HandlePaletteChanged()
        {
            RefreshVisual();
        }

        public bool HasConnection(Direction direction)
        {
            var currentIndex = (int)direction;
            var baseIndex = (currentIndex - RotationSteps + 4) % 4;
            return _baseConnections[baseIndex];
        }

        public bool TryRotateClockwise()
        {
            if (IsLocked)
            {
                return false;
            }

            RotationSteps = (RotationSteps + 1) % 4;
            RefreshVisual();
            return true;
        }

        public void SetActive(bool isActive)
        {
            _isActive = isActive;
            RefreshVisual();
        }

        public void TriggerPulse(float durationSeconds)
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
            }

            _pulseRoutine = StartCoroutine(PulseRoutine(durationSeconds));
        }

        private void OnMouseDown()
        {
        }

        private IEnumerator PulseRoutine(float durationSeconds)
        {
            _isPulsing = true;
            RefreshVisual();

            yield return new WaitForSecondsRealtime(Mathf.Max(0.02f, durationSeconds));

            _isPulsing = false;
            _pulseRoutine = null;
            RefreshVisual();
        }

        private void EnsureVisuals()
        {
            var collider2D = GetComponent<BoxCollider2D>();
            collider2D.size = Vector2.one * 0.95f;

            if (_bodyRenderer == null)
            {
                var body = new GameObject("Body");
                body.transform.SetParent(transform, false);
                _bodyRenderer = body.AddComponent<SpriteRenderer>();
                _bodyRenderer.sortingOrder = 0;
                _bodyRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
                body.transform.localScale = Vector3.one;
            }

            if (_endpointHighlightRenderer == null)
            {
                var highlight = new GameObject("EndpointHighlight");
                highlight.transform.SetParent(transform, false);
                _endpointHighlightRenderer = highlight.AddComponent<SpriteRenderer>();
                _endpointHighlightRenderer.sortingOrder = -1;
                _endpointHighlightRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
                _endpointHighlightRenderer.enabled = false;
                highlight.transform.localScale = new Vector3(1.16f, 1.16f, 1f);
            }

            if (_pipeRenderer == null)
            {
                var pipe = new GameObject("Pipe");
                pipe.transform.SetParent(transform, false);
                _pipeRenderer = pipe.AddComponent<SpriteRenderer>();
                _pipeRenderer.sortingOrder = 1;
                _pipeRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
                pipe.transform.localScale = Vector3.one;
            }

            CreateConnector(0, "ConnectorUp", new Vector3(0f, 0.31f, 0f), new Vector3(0.16f, 0.36f, 1f));
            CreateConnector(1, "ConnectorRight", new Vector3(0.31f, 0f, 0f), new Vector3(0.36f, 0.16f, 1f));
            CreateConnector(2, "ConnectorDown", new Vector3(0f, -0.31f, 0f), new Vector3(0.16f, 0.36f, 1f));
            CreateConnector(3, "ConnectorLeft", new Vector3(-0.31f, 0f, 0f), new Vector3(0.36f, 0.16f, 1f));
        }

        private void CreateConnector(int index, string objectName, Vector3 localPosition, Vector3 localScale)
        {
            if (_connectors[index] != null)
            {
                return;
            }

            var connector = new GameObject(objectName);
            connector.transform.SetParent(transform, false);
            connector.transform.localPosition = localPosition;
            connector.transform.localScale = localScale;

            var renderer = connector.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 1;
            renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
            _connectors[index] = renderer;
        }

        private void RefreshVisual()
        {
            if (_bodyRenderer == null)
            {
                return;
            }

            var bodyColor = BodyColor;
            if (IsLocked)
            {
                bodyColor = BodyLockedColor;
            }

            if (IsSource)
            {
                bodyColor = SourceColor;
            }
            else if (IsExit)
            {
                bodyColor = ExitColor;
            }
            else if (_isOverloaded)
            {
                bodyColor = Color.Lerp(BodyColor, GamePalette.Current.Overloaded, 0.45f);
            }
            else if (_isRequired)
            {
                bodyColor = Color.Lerp(BodyColor, GamePalette.Current.Required, 0.35f);
            }

            if (_isActive)
            {
                bodyColor = Color.Lerp(bodyColor, GamePalette.Current.Signal, 0.35f);
            }

            if (_isPulsing)
            {
                bodyColor = Color.Lerp(bodyColor, GamePalette.Current.Signal, 0.75f);
            }

            _bodyRenderer.color = bodyColor;

            if (_endpointHighlightRenderer != null)
            {
                if (IsSource || IsExit)
                {
                    _endpointHighlightRenderer.enabled = true;
                    var highlightColor = IsSource ? SourceHighlightColor : ExitHighlightColor;
                    if (_isActive || _isPulsing)
                    {
                        highlightColor = Color.Lerp(highlightColor, GamePalette.Current.Signal, 0.35f);
                        highlightColor.a = 0.75f;
                    }

                    _endpointHighlightRenderer.color = highlightColor;
                }
                else
                {
                    _endpointHighlightRenderer.enabled = false;
                }
            }

            var usePipeSprites = RuntimeSpriteFactory.HasPipelineSprites;
            var hasPipeVisual = false;
            if (_pipeRenderer != null)
            {
                if (usePipeSprites && TryGetPipeVisual(out var pipeSprite, out var pipeRotationZ))
                {
                    hasPipeVisual = true;
                    _pipeRenderer.enabled = true;
                    _pipeRenderer.sprite = pipeSprite;
                    _pipeRenderer.color = Color.white;
                    _pipeRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, pipeRotationZ);
                    var uniformScale = RuntimeSpriteFactory.PipelineUniformScale;
                    _pipeRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                }
                else
                {
                    _pipeRenderer.enabled = false;
                }
            }

            for (var i = 0; i < 4; i++)
            {
                var hasConnection = HasConnection((Direction)i);
                if (_connectors[i] != null)
                {
                    _connectors[i].enabled = (!usePipeSprites || !hasPipeVisual) && hasConnection;
                    _connectors[i].color = (_isActive || _isPulsing) ? GamePalette.Current.Signal : ConnectorColor;
                }
            }
        }

        private bool TryGetPipeVisual(out Sprite sprite, out float rotationZ)
        {
            var up = HasConnection(Direction.Up);
            var right = HasConnection(Direction.Right);
            var down = HasConnection(Direction.Down);
            var left = HasConnection(Direction.Left);

            var connectionCount = (up ? 1 : 0) + (right ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0);
            rotationZ = 0f;
            sprite = null;

            if (connectionCount >= 4)
            {
                sprite = RuntimeSpriteFactory.CrossSprite;
                return sprite != null;
            }

            if (connectionCount == 3)
            {
                sprite = RuntimeSpriteFactory.TeeSprite;

                if (!up)
                {
                    rotationZ = 0f;
                }
                else if (!right)
                {
                    rotationZ = 90f;
                }
                else if (!down)
                {
                    rotationZ = 180f;
                }
                else
                {
                    rotationZ = 270f;
                }

                rotationZ = (360f - rotationZ) % 360f;

                return sprite != null;
            }

            if (connectionCount == 2)
            {
                var isStraight = (up && down) || (left && right);
                if (isStraight)
                {
                    sprite = RuntimeSpriteFactory.StraightSprite;
                    rotationZ = (left && right) ? 0f : 90f;
                    return sprite != null;
                }

                sprite = RuntimeSpriteFactory.CornerSprite;
                if (down && right)
                {
                    rotationZ = 0f;
                }
                else if (right && up)
                {
                    rotationZ = 90f;
                }
                else if (up && left)
                {
                    rotationZ = 180f;
                }
                else
                {
                    rotationZ = 270f;
                }

                return sprite != null;
            }

            if (connectionCount == 1)
            {
                sprite = RuntimeSpriteFactory.StraightSprite;
                rotationZ = (left || right) ? 0f : 90f;
                return sprite != null;
            }

            if (connectionCount == 0 && IsLocked)
            {
                sprite = RuntimeSpriteFactory.CrossSprite;
                if (sprite == null)
                {
                    sprite = RuntimeSpriteFactory.StraightSprite;
                    rotationZ = 0f;
                }

                return sprite != null;
            }

            return false;
        }

    }
}
