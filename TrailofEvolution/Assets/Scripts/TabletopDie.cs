using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrailOfMutation
{
    public sealed class TabletopDie : MonoBehaviour
    {
        const int DieLayer = 29;
        static readonly Vector3[] Faces = { Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down };
        readonly List<Material> materials = new List<Material>();
        GameObject stage;
        Transform die, shadow;
        Camera camera3D;
        RenderTexture target;
        Mesh roundedMesh;
        public int Value { get; private set; } = 1;
        public bool IsRolling { get; private set; }
        public int UpwardValue
        {
            get {
                int result = 1; float best = -2;
                for (int i = 0; i < 6; i++) { float dot = Vector3.Dot(die.rotation * Faces[i], Vector3.up); if (dot > best) { best = dot; result = i + 1; } }
                return result;
            }
        }
        Material MakeMaterial(Color color, bool unlit = false)
        {
            var material = new Material(Resources.Load<Material>(unlit ? "Board/BoardPaint" : "Board/PawnResin"));
            material.color = color;
            if (!unlit) { material.SetFloat("_Smoothness", .48f); material.SetFloat("_Metallic", .03f); }
            materials.Add(material); return material;
        }
        GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.layer = DieLayer;
            g.transform.SetParent(parent, false); g.transform.localPosition = position; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material; Destroy(g.GetComponent<Collider>()); return g;
        }
        public void Initialize()
        {
            stage = new GameObject("Dice studio"); stage.transform.position = new Vector3(200, 0, 0);
            var body = new GameObject("Rounded ivory die", typeof(MeshFilter), typeof(MeshRenderer));
            body.layer = DieLayer; die = body.transform; die.SetParent(stage.transform, false);
            roundedMesh = RoundedCube(.12f, 12);
            body.GetComponent<MeshFilter>().sharedMesh = roundedMesh;
            body.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(TrailUI.Hex("FFF1CF"));
            var ink = MakeMaterial(TrailUI.Hex("223D35"));
            int[][] dots = { new[] {4}, new[] {0,8}, new[] {0,4,8}, new[] {0,2,6,8}, new[] {0,2,4,6,8}, new[] {0,2,3,5,6,8} };
            for (int f = 0; f < 6; f++)
            {
                Basis(Faces[f], out var u, out var v);
                foreach (int dot in dots[f])
                {
                    var pip = Primitive("Face " + (f + 1) + " pip", PrimitiveType.Sphere, die,
                        Faces[f] * .501f + u * ((dot % 3 - 1) * .23f) + v * ((dot / 3 - 1) * .23f),
                        new Vector3(.125f, .018f, .125f), ink);
                    pip.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Faces[f]);
                }
            }
            shadow = Primitive("Soft contact shadow", PrimitiveType.Sphere, stage.transform, new Vector3(0,.006f,0),
                new Vector3(1.25f,.008f,1.1f), MakeMaterial(TrailUI.Hex("D9D1BC"), true)).transform;
            var cameraObject = new GameObject("Dice camera", typeof(Camera)); cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 7, -8); cameraObject.transform.LookAt(stage.transform.position + Vector3.up * .3f);
            camera3D = cameraObject.GetComponent<Camera>(); camera3D.orthographic = true; camera3D.orthographicSize = 1.65f;
            camera3D.cullingMask = 1 << DieLayer; camera3D.clearFlags = CameraClearFlags.SolidColor; camera3D.backgroundColor = TrailUI.Paper;
            camera3D.nearClipPlane = .1f; camera3D.farClipPlane = 30;
            target = new RenderTexture(880,480,24); target.antiAliasing = 4; target.Create(); camera3D.targetTexture = target;
            var image = gameObject.AddComponent<RawImage>(); image.texture = target; image.raycastTarget = false;
            var lightObject = new GameObject("Dice softbox", typeof(Light)); lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(40,-30,0);
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f; light.cullingMask = 1 << DieLayer;
            if (Camera.main) Camera.main.cullingMask &= ~(1 << DieLayer);
            SetFace(1);
        }
        static void Basis(Vector3 normal, out Vector3 u, out Vector3 v)
        {
            u = Mathf.Abs(normal.y) > .9f ? Vector3.right : Vector3.Cross(Vector3.up, normal).normalized;
            v = Vector3.Cross(normal, u);
        }
        static Mesh RoundedCube(float radius, int segments)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var triangles = new List<int>();
            foreach (var normal in Faces)
            {
                Basis(normal, out var u, out var v); int start = vertices.Count;
                for (int y = 0; y <= segments; y++) for (int x = 0; x <= segments; x++)
                {
                    Vector3 p = normal * .5f + u * ((float)x / segments - .5f) + v * ((float)y / segments - .5f);
                    float inner = .5f - radius;
                    var core = new Vector3(Mathf.Clamp(p.x,-inner,inner), Mathf.Clamp(p.y,-inner,inner), Mathf.Clamp(p.z,-inner,inner));
                    var direction = (p - core).normalized;
                    vertices.Add(core + direction * radius); normals.Add(direction);
                }
                for (int y = 0; y < segments; y++) for (int x = 0; x < segments; x++)
                {
                    int a = start + y * (segments + 1) + x, b = a + segments + 1;
                    triangles.AddRange(new[] { a, a+1, b, a+1, b+1, b });
                }
            }
            var mesh = new Mesh { name = "Smooth bevel dice mesh" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds(); return mesh;
        }
        Quaternion FaceRotation(int value) => Quaternion.AngleAxis(24,Vector3.up) * Quaternion.FromToRotation(Faces[value-1], Vector3.up);
        public void SetFace(int value)
        {
            Value = Mathf.Clamp(value,1,6); die.localRotation = FaceRotation(Value); die.localPosition = new Vector3(.3f,.51f,0);
            shadow.localPosition = new Vector3(.3f,.006f,0); shadow.localScale = new Vector3(1.25f,.008f,1.1f);
        }
        public IEnumerator Roll(int value, int seed)
        {
            IsRolling = true; var random = new System.Random(seed);
            var spin = new Vector3(720 + random.Next(360), 540 + random.Next(360), 720 + random.Next(360));
            Quaternion destination = FaceRotation(value), lastSpin = Quaternion.Euler(spin);
            const float duration = 1.65f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration, easing = 1 - Mathf.Pow(1-t,3);
                die.localRotation = t < .72f ? Quaternion.Euler(spin * (t/.72f)) : Quaternion.Slerp(lastSpin, destination, Mathf.SmoothStep(0,1,(t-.72f)/.28f));
                float support = .5f * (Mathf.Abs((die.localRotation * Vector3.right).y) + Mathf.Abs((die.localRotation * Vector3.up).y) + Mathf.Abs((die.localRotation * Vector3.forward).y));
                float bounce = .85f * (1-t) * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4));
                float x = Mathf.Lerp(-1.6f,.3f,easing), z = Mathf.Sin(t * Mathf.PI * 3) * (1-t) * .4f;
                die.localPosition = new Vector3(x, support + bounce + .01f, z);
                shadow.localPosition = new Vector3(x,.006f,z);
                shadow.localScale = new Vector3(1.25f + bounce*.4f,.008f,1.1f + bounce*.4f);
                yield return null;
            }
            SetFace(value); IsRolling = false;
        }
        public void RenderForCapture()
        {
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera3D,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
        }
        void OnDestroy()
        {
            if (stage) Destroy(stage);
            if (target) { target.Release(); Destroy(target); }
            if (roundedMesh) Destroy(roundedMesh);
            foreach (var material in materials) Destroy(material);
        }
    }
}
