using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrailOfMutation
{
    // Real meshes and a dedicated camera keep the tabletop lit independently of the overlay UI.
    public sealed class TabletopBoard : MonoBehaviour
    {
        const int BoardLayer = 30;
        readonly List<Material> materials = new List<Material>();
        readonly List<RectTransform> anchors = new List<RectTransform>();
        readonly List<Transform> pawns = new List<Transform>();
        readonly List<float> hops = new List<float>();
        GameObject stage;
        RenderTexture target;
        Camera boardCamera;
        public Rect MapViewportBounds
        {
            get {
                Vector2 min = new Vector2(1,1), max = Vector2.zero;
                for (int i = 0; i < 4; i++) {
                    Vector2 p = boardCamera.WorldToViewportPoint(stage.transform.position + new Vector3(i%2 == 0 ? -7.55f : 7.55f, 0, i<2 ? -5.05f : 5.05f));
                    min = Vector2.Min(min,p); max = Vector2.Max(max,p);
                }
                return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            }
        }
        Material Material(Color color, bool unlit = false)
        {
            var template = Resources.Load<Material>(unlit ? "Board/BoardPaint" : "Board/PawnResin");
            var m = template ? new Material(template) : new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            m.color = color;
            if (!unlit) { m.SetFloat("_Smoothness", .32f); m.SetFloat("_Metallic", .12f); }
            materials.Add(m); return m;
        }
        GameObject Part(Transform parent, string name, PrimitiveType shape, Vector3 p, Vector3 size, Material mat)
        {
            var g = GameObject.CreatePrimitive(shape); g.name = name; g.layer = BoardLayer;
            g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(g.GetComponent<Collider>()); return g;
        }
        public void Initialize()
        {
            stage = new GameObject("Hand-painted tabletop stage"); stage.transform.position = new Vector3(100, 0, 0);
            var edge = Material(TrailUI.Hex("553C25"));
            Part(stage.transform, "Thick wooden board", PrimitiveType.Cube, new Vector3(0,-.12f,0), new Vector3(15.1f,.24f,10.1f), edge);
            var paint = Material(Color.white, true); paint.mainTexture = Resources.Load<Texture2D>("Board/EvolutionMap");
            var surface = Part(stage.transform, "Illustrated paper", PrimitiveType.Quad, new Vector3(0,.015f,0), new Vector3(15,10,1), paint);
            surface.transform.localRotation = Quaternion.Euler(90,0,0);
            var cameraObject = new GameObject("Tabletop camera", typeof(Camera)); cameraObject.transform.SetParent(stage.transform, false);
            const float tilt = 25 * Mathf.Deg2Rad;
            cameraObject.transform.localPosition = new Vector3(0, Mathf.Cos(tilt) * 20, -Mathf.Sin(tilt) * 20);
            cameraObject.transform.LookAt(stage.transform.position);
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = false; camera.fieldOfView = 40;
            boardCamera = camera;
            camera.cullingMask = 1 << BoardLayer; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TrailUI.Hex("102E2C"); camera.nearClipPlane = .1f; camera.farClipPlane = 40;
            target = new RenderTexture(2048, 1557, 24); target.antiAliasing = 2; target.Create(); camera.targetTexture = target;
            var view = TrailUI.Rect("Live 3D tabletop", transform, 0,0,1460,1110).gameObject.AddComponent<RawImage>();
            view.texture = target; view.raycastTarget = true;
            view.gameObject.AddComponent<BoardNavigation>().Initialize(this);
            var lightObject = new GameObject("Warm tabletop light", typeof(Light)); lightObject.transform.SetParent(stage.transform,false);
            lightObject.transform.rotation = Quaternion.Euler(48,-30,0);
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.6f;
            light.color = new Color(1,.91f,.76f); light.cullingMask = 1 << BoardLayer; light.shadows = LightShadows.Soft;
            if (Camera.main) Camera.main.cullingMask &= ~(1 << BoardLayer);
        }
        public void AddPawn(RectTransform anchor, int animal, Color color)
        {
            anchors.Add(anchor);
            hops.Add(0);
            var pawn = new GameObject(new[] {"FERN deer miniature", "EMBER fox miniature", "TIDAL turtle miniature"}[animal]);
            pawn.transform.SetParent(stage.transform,false); pawns.Add(pawn.transform);
            var resin = Material(color); var dark = Material(TrailUI.Hex("252B23")); var cream = Material(TrailUI.Hex("F8E9CA"));
            Part(pawn.transform,"Round weighted plinth",PrimitiveType.Cylinder,new Vector3(0,.09f,0),new Vector3(.7f,.09f,.7f),resin);
            // A subtle contact shadow remains visible even when the quality preset disables realtime shadows.
            var shadow = Material(new Color(.16f,.21f,.13f));
            Part(pawn.transform,"Contact shadow",PrimitiveType.Cylinder,new Vector3(.07f,.025f,.06f),new Vector3(.76f,.008f,.62f),shadow);
            if(animal == 2)
            {
                Part(pawn.transform,"Domed shell",PrimitiveType.Sphere,new Vector3(0,.38f,0),new Vector3(.65f,.40f,.8f),resin);
                Part(pawn.transform,"Shell ridge",PrimitiveType.Sphere,new Vector3(0,.54f,0),new Vector3(.40f,.14f,.55f),resin);
                Part(pawn.transform,"Turtle head",PrimitiveType.Sphere,new Vector3(0,.35f,-.48f),new Vector3(.28f,.24f,.3f),resin);
                for(int i=0;i<4;i++) Part(pawn.transform,"Flipper",PrimitiveType.Sphere,new Vector3(i%2==0?-.34f:.34f,.24f,i<2?-.25f:.25f),new Vector3(.28f,.12f,.32f),resin);
                Eyes(pawn.transform, dark, .35f, -.6f, .085f);
            }
            else
            {
                Part(pawn.transform,"Body",PrimitiveType.Sphere,new Vector3(0,.63f,0),new Vector3(.40f,.48f,.72f),resin);
                for(int i=0;i<4;i++) Part(pawn.transform,"Leg",PrimitiveType.Capsule,new Vector3(i%2==0?-.14f:.14f,.36f,i<2?-.23f:.23f),new Vector3(.12f,.22f,.13f),resin);
                Part(pawn.transform,"Neck",PrimitiveType.Capsule,new Vector3(0,.85f,-.26f),new Vector3(.23f,.30f,.23f),resin);
                Part(pawn.transform,"Head",PrimitiveType.Sphere,new Vector3(0,1.08f,-.35f),new Vector3(.37f,.34f,.40f),resin);
                Part(pawn.transform,"Muzzle",PrimitiveType.Sphere,new Vector3(0,1.01f,-.56f),new Vector3(.24f,.18f,.30f),animal==1?cream:resin);
                Part(pawn.transform,"Nose",PrimitiveType.Sphere,new Vector3(0,1.04f,-.70f),new Vector3(.10f,.08f,.07f),dark);
                Eyes(pawn.transform,dark,1.12f,-.505f,.13f);
                for(int side=-1;side<=1;side+=2)
                {
                    var ear=Part(pawn.transform,"Pointed ear",PrimitiveType.Sphere,new Vector3(side*.17f,1.29f,-.30f),new Vector3(.13f,.32f,.12f),resin);
                    ear.transform.localRotation=Quaternion.Euler(0,0,side*-22);
                    if(animal==0)
                    {
                        for(int branch=0;branch<3;branch++)
                        {
                            var antler=Part(pawn.transform,"Antler branch",PrimitiveType.Capsule,new Vector3(side*(.16f+branch*.06f),1.48f+branch*.12f,-.21f),new Vector3(.055f,.17f,.055f),resin);
                            antler.transform.localRotation=Quaternion.Euler(15,0,side*(branch==1?55:15));
                        }
                    }
                }
                var tail=Part(pawn.transform,"Tail",PrimitiveType.Sphere,new Vector3(.08f,.73f,.48f),animal==1?new Vector3(.30f,.34f,.65f):new Vector3(.12f,.15f,.25f),resin);
                tail.transform.localRotation=Quaternion.Euler(-35,20,0);
                if(animal==1) Part(pawn.transform,"Cream tail tip",PrimitiveType.Sphere,new Vector3(.13f,.92f,.70f),new Vector3(.20f,.22f,.25f),cream);
            }
            pawn.transform.localRotation = Quaternion.Euler(0,-28,0);
            pawn.transform.localScale = Vector3.one * .82f;
        }
        void Eyes(Transform parent, Material mat,float y,float z,float width)
        {
            for(int i=-1;i<=1;i+=2) Part(parent,"Inset eye",PrimitiveType.Sphere,new Vector3(width*i,y,z),new Vector3(.055f,.065f,.05f),mat);
        }
        void LateUpdate()
        {
            for(int i=0;i<pawns.Count;i++)
            {
                var p = anchors[i].anchoredPosition;
                pawns[i].localPosition = new Vector3((p.x/1080f-.5f)*15,.03f+hops[i],(.5f+p.y/690f)*10);
            }
        }
        public void SetHop(int index, float height) { hops[index] = height; }
        public void RenderForCapture()
        {
            LateUpdate();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(boardCamera,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
        }
        void OnDestroy()
        {
            if(stage) Destroy(stage);
            if(target) { target.Release(); Destroy(target); }
            foreach(var m in materials) Destroy(m);
        }
    }
}
