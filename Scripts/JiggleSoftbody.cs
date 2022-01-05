using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace JigglePhysics {
    public class JiggleSoftbody : MonoBehaviour {
        public bool cullOffscreen = true;
        public enum UpdateType {
            FixedUpdate,
            Update,
            LateUpdate,
        }
        private float timeAccumulator;
        public UpdateType updateMode = UpdateType.LateUpdate;
        public float randomization = 0.1f;
        [System.Serializable]
        public class SoftbodyZone {
            [System.Serializable]
            public class UnityEventVector3 : UnityEvent<Vector3> {}
            public Transform origin;
            public Vector3 offset;
            public float radius;
            public float amplitude;
            public float elastic;
            public float friction;
            //public float maximumAcceleration=1f;
            public Vector3 gravity;
            public Color colorMask;
            [HideInInspector]
            public Vector3 lastPosition;
            [HideInInspector]
            public Vector3 lastVelocityGuess;
            [HideInInspector]
            public Vector3 velocity;
            [HideInInspector]
            public Vector3 virtualPos;
            public UnityEventVector3 jiggleEvent;
            public void Friction(float dt) {
                float speed = velocity.magnitude;
                if (speed<Mathf.Epsilon) {
                    return;
                }
                float drop = friction * speed * dt;
                float newSpeed = speed - drop;
                if (newSpeed<0) {
                    newSpeed = 0;
                }
                newSpeed /= speed;
                velocity *= newSpeed;
            }
            public void Gravity(float dt, float scale) {
                velocity -= gravity * dt * scale;
            }
            public void CalculateAcceleration(float dt) {
                Vector3 velocityGuess = (origin.TransformPoint(offset) - lastPosition)/dt;
                Vector3 newVelocity = (velocityGuess-lastVelocityGuess);
                // SINWAVE BASED MAXIMUM ACCELLERATION APPROACH
                //newVelocity = newVelocity.normalized * Mathf.Sin(Mathf.Clamp(newVelocity.magnitude*(Mathf.PI/2f), -1f, 1f)*(0.5f/maximumAcceleration))*maximumAcceleration;
                velocity += newVelocity;
                lastVelocityGuess = velocityGuess;
            }
            public void Acceleration(float dt) {
                velocity += virtualPos * elastic * dt * 100f;
                virtualPos -= velocity * dt;
                lastPosition = origin.TransformPoint(offset);
            }
            public void Pack(ref Vector4[] packTarget, SkinnedMeshRenderer r, int index, float scale) {
                packTarget[index * 3] = colorMask;
                packTarget[index * 3 + 1] = r.rootBone.InverseTransformPoint(origin.TransformPoint(offset))*scale;
                packTarget[index * 3 + 1].w = origin.lossyScale.y*radius;
                packTarget[index * 3 + 2] = r.rootBone.InverseTransformVector(virtualPos);
                packTarget[index * 3 + 2].w = amplitude * scale;
            }
            public void Draw() {
                if (origin == null) {
                    return;
                }
                Gizmos.color = new Color(colorMask.r, colorMask.g, colorMask.b, Mathf.Clamp(colorMask.r + colorMask.g + colorMask.b + colorMask.a, 0f, 0.5f));
                Gizmos.DrawSphere(origin.TransformPoint(offset), origin.lossyScale.y*radius);
            }
        }
        private Vector4[] vectorsToSend;
        public List<SkinnedMeshRenderer> targetRenderers;
        private Dictionary<SkinnedMeshRenderer, MaterialPropertyBlock> blockCache;
        public List<SoftbodyZone> zones;
        public MaterialPropertyBlock GetBlock(SkinnedMeshRenderer r) {
            if (!blockCache.ContainsKey(r)) {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                blockCache.Add(r, block);
            }
            return blockCache[r];
        }
        public void Awake() {
            foreach (SoftbodyZone zone in zones) {
                zone.amplitude *= UnityEngine.Random.Range(1f - randomization, 1f + randomization);
                zone.elastic *= UnityEngine.Random.Range(1f - randomization, 1f + randomization);
                zone.friction *= UnityEngine.Random.Range(1f - randomization, 1f + randomization);
            }
        }
        public void Start() {
            vectorsToSend = new Vector4[24];
            blockCache = new Dictionary<SkinnedMeshRenderer, MaterialPropertyBlock>();
            foreach (SkinnedMeshRenderer r in targetRenderers) {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                blockCache.Add(r, block);
            }
            foreach( SoftbodyZone zone in zones) {
                zone.lastPosition = zone.origin.TransformPoint(zone.offset);
            }
        }
        public void LateUpdate() {
            if (updateMode != UpdateType.LateUpdate || Time.deltaTime == 0f) {
                return;
            }
            foreach (SoftbodyZone zone in zones) {
                zone.CalculateAcceleration(Time.deltaTime);
            }
            float frameTime = Mathf.Min(Time.deltaTime,Time.maximumDeltaTime);
            timeAccumulator += frameTime;
            while (timeAccumulator >= Time.fixedDeltaTime) {
                Process(Time.fixedDeltaTime);
                timeAccumulator -= Time.fixedDeltaTime;
            }
            Process(timeAccumulator);
            timeAccumulator = 0f;
            SendData();
        }
        public void FixedUpdate() {
            if (updateMode != UpdateType.FixedUpdate || Time.deltaTime == 0f) {
                return;
            }
            foreach (SoftbodyZone zone in zones) {
                zone.CalculateAcceleration(Time.deltaTime);
            }
            Process(Time.deltaTime);
            SendData();
        }
        public void Update() {
            if (updateMode != UpdateType.Update || Time.deltaTime == 0f) {
                return;
            }
            foreach (SoftbodyZone zone in zones) {
                zone.CalculateAcceleration(Time.deltaTime);
            }
            float frameTime = Mathf.Min(Time.deltaTime,Time.maximumDeltaTime);
            timeAccumulator += frameTime;
            while (timeAccumulator >= Time.fixedDeltaTime) {
                Process(Time.fixedDeltaTime);
                timeAccumulator -= Time.fixedDeltaTime;
            }
            Process(timeAccumulator);
            timeAccumulator = 0f;
            SendData();
        }
        public void Process(float dt) {
            if (dt == 0f) {
                return;
            }
            foreach (SoftbodyZone zone in zones) {
                zone.Friction(dt);
                zone.Gravity(dt, transform.lossyScale.x);
                zone.Acceleration(dt);
            }
        }
        public void SendData() {
            foreach (SkinnedMeshRenderer r in targetRenderers) {
                if (!r.isVisible && cullOffscreen) {
                    continue;
                }
                int i = 0;
                foreach (SoftbodyZone zone in zones) {
                    zone.Pack(ref vectorsToSend, r, i++, transform.lossyScale.x);
                }
                GetBlock(r).SetVectorArray("_SoftbodyArray", vectorsToSend);
                r.SetPropertyBlock(GetBlock(r));
            }
            foreach (SoftbodyZone zone in zones) {
                zone.jiggleEvent.Invoke(zone.virtualPos);
            }
        }
        public void OnDrawGizmosSelected() {
            if (zones == null) {
                return;
            }
            foreach (SoftbodyZone zone in zones) {
                zone?.Draw();
            }
        }
        public Vector3 TransformPoint(Vector3 wpos, Color color) {
            Vector3 offset = Vector3.zero;
            foreach(SoftbodyZone zone in zones) {
                float mask = Mathf.Clamp01(Vector4.Dot(color, zone.colorMask));
                float dist = Vector3.Distance(targetRenderers[0].rootBone.InverseTransformPoint(wpos), targetRenderers[0].rootBone.InverseTransformPoint(zone.origin.position)) / (zone.radius*zone.origin.lossyScale.x);
                float effect = Mathf.Clamp01(1f - dist * dist) * mask;
                offset += zone.virtualPos * targetRenderers[0].rootBone.lossyScale.x * effect * zone.amplitude;
            }
            return wpos + offset;
        }
        public void OnValidate() {
            if (zones == null) {
                return;
            }
            foreach(SoftbodyZone zone in zones) {
                if (zone == null) {
                    continue;
                }
                // If we were just created, do some okay defaults
                if (zone.friction == 0f && zone.radius == 0f && zone.amplitude == 0f && zone.elastic == 0f) {
                    zone.radius = 0.5f;
                    zone.amplitude = 1f;
                    zone.elastic = 4f;
                    zone.friction = 5f;
                }
            }
        }
    }
}
