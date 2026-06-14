using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Overlay GL des contours de colliders 2D (toggle H par défaut, vue Game).
    /// </summary>
    public class HitboxDebugOverlay : MonoBehaviour
    {
        public static bool Enabled = false;
        public static void Toggle() => Enabled = !Enabled;

        [SerializeField] private Color _circleColor = new Color(0.2f, 1f, 0.3f, 0.9f);
        [SerializeField] private Color _boxColor = new Color(1f, 0.5f, 0.1f, 0.9f);
        [SerializeField] private int _circleSegments = 40;
        [SerializeField] private KeyCode _toggleKey = KeyCode.H;

        private static readonly List<Collider2D> _colliders = new List<Collider2D>(64);
        private Material _mat;

        public static void Register(Collider2D c)
        {
            if (c != null && !_colliders.Contains(c))
                _colliders.Add(c);
        }

        public static void Unregister(Collider2D c)
        {
            _colliders.Remove(c);
        }

        private void Awake()
        {
            _mat = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _mat.SetInt("_ZWrite", 0);
        }

        private void Update()
        {
            if (_toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
                Toggle();
        }

        private void OnRenderObject()
        {
            if (!Enabled || _mat == null) return;

            _mat.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            for (int i = _colliders.Count - 1; i >= 0; i--)
            {
                Collider2D col = _colliders[i];
                if (col == null)
                {
                    _colliders.RemoveAt(i);
                    continue;
                }

                if (!col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                if (col is CircleCollider2D circle)
                    DrawCircle(circle);
                else if (col is BoxCollider2D box)
                    DrawBox(box);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void DrawCircle(CircleCollider2D circle)
        {
            GL.Color(_circleColor);
            Transform t = circle.transform;
            Vector3 ls = t.lossyScale;
            float r = circle.radius * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
            Vector3 c = t.TransformPoint(circle.offset);
            float step = Mathf.PI * 2f / _circleSegments;
            Vector3 prev = c + new Vector3(r, 0f, 0f);

            for (int s = 1; s <= _circleSegments; s++)
            {
                float a = step * s;
                Vector3 next = c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                GL.Vertex(prev);
                GL.Vertex(next);
                prev = next;
            }
        }

        private void DrawBox(BoxCollider2D box)
        {
            GL.Color(_boxColor);
            Transform t = box.transform;
            Vector2 h = box.size * 0.5f;
            Vector3 p0 = t.TransformPoint(box.offset + new Vector2(-h.x, -h.y));
            Vector3 p1 = t.TransformPoint(box.offset + new Vector2(h.x, -h.y));
            Vector3 p2 = t.TransformPoint(box.offset + new Vector2(h.x, h.y));
            Vector3 p3 = t.TransformPoint(box.offset + new Vector2(-h.x, h.y));
            GL.Vertex(p0);
            GL.Vertex(p1);
            GL.Vertex(p1);
            GL.Vertex(p2);
            GL.Vertex(p2);
            GL.Vertex(p3);
            GL.Vertex(p3);
            GL.Vertex(p0);
        }
    }
}
