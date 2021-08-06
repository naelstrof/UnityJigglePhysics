using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
    public class JiggleBone : MonoBehaviour {
        public enum UpdateType {
            FixedUpdate,
            Update,
            LateUpdate,
        }

        public bool cullOffscreen = false;
        public List<Renderer> targetRenderers;
        public UpdateType updateMode = UpdateType.LateUpdate;
        public Transform root;
        public AnimationCurve elasticity;
        public float elasticityMultiplier;
        public AnimationCurve friction;
        public float frictionMultiplier;
        public bool rotateRoot = true;

        private float internalActive = 1f;
        public float active {
            get {
                return internalActive;
            }
            set {
                if (bones != null && Mathf.Approximately(value, 0f)) {
                    for (int i = 0; i < bones.Count; i++) {
                        VirtualBone b = bones[i];
                        // Purely virtual particle!
                        if (b.self == null) {
                            continue;
                        }
                        b.self.localPosition = b.localStartPos;
                        b.self.localRotation = b.localStartRot;
                    }
                }
                internalActive = Mathf.Clamp01(value);
            }
        }
        public bool accelerationBased = true;
        [Range(1, 4f)]
        public float maxStretch = 1.1f;
        [Range(0, 1f)]
        public float maxSquish = 0.1f;
        public Vector3 gravity;
        public List<Transform> excludedTransforms;
        private int depth;
        private List<VirtualBone> bones;
        private List<VirtualBone> previousFrameBones;
        private Vector3 lastRootPosition;
        private Vector3 lastVelocityGuess;
        private Vector3 positionDiff;
        private Vector3 accelerationGuess;
        public static int GetRootDistance(Transform root, Transform t) {
            int a = 0;
            Transform findRoot = t;
            while (findRoot != root && (findRoot.parent != root || findRoot.parent == null)) {
                a++;
                findRoot = findRoot.parent;
            }
            return a;
        }
        public void BuildVirtualBoneTree(List<VirtualBone> list, Transform root, Transform t, int depth, VirtualBone parent = null) {
            if (parent == null) {
                parent = new VirtualBone(t, null, root, (float)GetRootDistance(root, t) / (float)depth);
                list.Add(parent);
            }
            for (int i = 0; i < t.childCount; i++) {
                if (excludedTransforms.Contains(t.GetChild(i))) {
                    continue;
                }
                VirtualBone child = new VirtualBone(t.GetChild(i), parent, root, (float)GetRootDistance(root, t.GetChild(i)) / (float)depth);
                list.Add(child);
                BuildVirtualBoneTree(list, root, t.GetChild(i), depth, child);
            }
            if (t.childCount == 0) {
                VirtualBone child = new VirtualBone(null, parent, root, 1f);
                list.Add(child);
            }
        }
        public static int GetDeepestChild(Transform t, int currentDepth = 0) {
            if (t.childCount == 0) {
                return currentDepth;
            }
            int max = 0;
            for (int i = 0; i < t.childCount; i++) {
                max = Mathf.Max(GetDeepestChild(t.GetChild(i), currentDepth + 1), max);
            }
            return max;
        }
        public class VirtualBone {
            float endExtensionDistance = 0.25f;
            public VirtualBone(Transform s, VirtualBone parent, Transform root, float chainPos) {
                self = s;
                if (self != null) {
                    position = self.position;
                    localStartPos = s.localPosition;
                    localStartRot = s.localRotation;
                } else {
                    position = root.position + root.up * endExtensionDistance;
                    localStartPos = Vector3.up * endExtensionDistance;
                }
                this.parent = parent;
                if (parent != null) {
                    parent.children.Add(this);
                }
                chainPosition = chainPos;
                children = new List<VirtualBone>();
            }
            public void Friction(JiggleBone jiggle, float dt) {
                float speed = velocity.magnitude;
                if (speed < Mathf.Epsilon || speed == 0f) {
                    return;
                }
                float drop = jiggle.friction.Evaluate(chainPosition) * jiggle.frictionMultiplier * speed * dt;
                float newSpeed = speed - drop;
                if (newSpeed < 0) {
                    newSpeed = 0;
                }
                newSpeed /= speed;
                velocity *= newSpeed;
            }
            public void Gravity(JiggleBone jiggle, float dt) {
                velocity += jiggle.gravity * dt;
            }
            public void Acceleration(JiggleBone jiggle, float dt) {
                if (parent == null) {
                    return;
                }
                // Undo the rotation adjustment of our parent (so it's back to a neutral rotation), then use our localStartPos to figure out where we *should* accelerate towards.
                Vector3 wantedPos = (Quaternion.Inverse(parent.rotationAdjust) * parent.self.TransformVector(localStartPos) + parent.self.position) - position;
                Vector3 force = wantedPos * jiggle.elasticity.Evaluate(chainPosition) * jiggle.elasticityMultiplier;

                velocity += force * 100f * dt;
                //parent.velocity -= force * 50f * dt;
            }
            public void Projection(JiggleBone j) {
                if (parent != null) {
                    float d = Vector3.Distance(position, parent.position);
                    float wantedDistance = parent.self.TransformVector(localStartPos).magnitude;
                    float bounciness = 0.1f;
                    if (d > wantedDistance * j.maxStretch) {
                        // Bounce with some velocity loss!
                        Vector3 normal = (parent.position - position).normalized;
                        if (Vector3.Dot(velocity, normal) < 0f) {
                            velocity = Vector3.Lerp(Vector3.ProjectOnPlane(velocity, normal), Vector3.Reflect(velocity, normal), bounciness);
                        }
                        position = parent.position + (position - parent.position).normalized * wantedDistance * j.maxStretch;
                    } else if (d < wantedDistance * (1f - j.maxSquish)) {
                        Vector3 normal = (position - parent.position).normalized;
                        // Bounce with some velocity loss!
                        if (Vector3.Dot(velocity, normal) < 0f) {
                            velocity = Vector3.Lerp(Vector3.ProjectOnPlane(velocity, normal), Vector3.Reflect(velocity, normal), bounciness);
                        }
                        position = parent.position + (position - parent.position).normalized * wantedDistance * (1f - j.maxSquish);
                    }
                }
            }
            public void SetPos(JiggleBone j, float dt) {
                position += velocity * dt;
            }
            public VirtualBone parent;
            public List<VirtualBone> children;
            public Transform self;
            public float chainPosition;
            public Quaternion localStartRot;
            public Vector3 localStartPos;
            public Quaternion rotationAdjust;

            public Vector3 velocity;
            public Vector3 position;
        }
        public void Regenerate() {
            depth = GetDeepestChild(root);
            bones = new List<VirtualBone>();
            BuildVirtualBoneTree(bones, root, root, depth);
        }

        public void Awake() {
            if (bones == null) {
                Regenerate();
            }
        }
        public void Start() {
            lastRootPosition = root.position;
        }
        public void RecalculateAcceleration(float dt) {
            // Don't recalculate if time isn't moving.
            if (dt < Mathf.Epsilon || dt == 0f) {
                return;
            }
            positionDiff = (root.position - lastRootPosition);
            lastRootPosition = root.position;
            Vector3 velocityGuess = positionDiff / dt;
            accelerationGuess = (velocityGuess - lastVelocityGuess);
            lastVelocityGuess = velocityGuess;
            if (accelerationBased) {
                foreach (VirtualBone b in bones) {
                    b.velocity -= accelerationGuess;
                    b.position += positionDiff;
                }
            }
        }
        public void Process(float dt) {
            if (cullOffscreen) {
                bool shouldCull = true;
                foreach(Renderer r in targetRenderers) {
                    if (r == null || r.isVisible) {
                        shouldCull = false;
                    }
                }
                if (shouldCull) {
                    return;
                }
            }
            if (!isActiveAndEnabled || dt < Mathf.Epsilon || dt == 0 || Mathf.Approximately(active, 0f)) {
                return;
            }
            // Velocity update
            foreach (VirtualBone b in bones) {
                if (float.IsNaN(b.position.x + b.position.y + b.position.z)) {
                    if (b.self != null) {
                        b.position = b.self.position;
                    } else {
                        b.position = Vector3.zero;
                    }
                    b.velocity = Vector3.zero;
                }
                // Make sure the root bone is pinned.
                if (b.parent == null) {
                    b.position = b.self.position;
                    continue;
                }
                b.Friction(this, dt);
                b.Gravity(this, dt);
                b.Acceleration(this, dt);
            }
            // Add velocity to position (then project it to make sure it doesn't stretch too much).
            foreach (VirtualBone b in bones) {
                b.SetPos(this, dt);
            }
            // Transforms update, have to set our positions carefully down the chain since each rotation breaks all the positions of the children bones.
            for (int i = 0; i < bones.Count; i++) {
                VirtualBone b = bones[i];
                // Purely virtual particle!
                if (b.self == null) {
                    continue;
                }
                if (b.parent == null) {
                    b.position = b.self.position;
                } else {
                    Vector3 wantedPosition = Vector3.Lerp(b.self.parent.TransformPoint(b.localStartPos), b.position, active);
                    b.self.position = Vector3.Lerp(b.self.position, wantedPosition, active);
                }

                if (b.children.Count <= 0) {
                    continue;
                }
                if (b.self == root && !rotateRoot) {
                    continue;
                }
                b.self.localRotation = b.localStartRot;
                b.rotationAdjust = Quaternion.Lerp(Quaternion.identity, Quaternion.FromToRotation(b.self.TransformDirection(b.children[0].localStartPos.normalized), (b.children[0].position - b.self.position).normalized), active);
                b.self.rotation = b.rotationAdjust * b.self.rotation;
            }
        }
        public void LateUpdate() {
            if (updateMode != UpdateType.LateUpdate || Time.deltaTime == 0f) {
                return;
            }
            RecalculateAcceleration(Time.deltaTime);
            float timeToPass = Mathf.Min(Time.deltaTime, Time.maximumDeltaTime);
            while (timeToPass > Time.fixedDeltaTime) {
                Process(Time.fixedDeltaTime);
                timeToPass -= Time.fixedDeltaTime;
            }
            if (timeToPass > 0f) {
                Process(timeToPass);
            }
            foreach (VirtualBone b in bones) {
                b.Projection(this);
            }
        }
        public void FixedUpdate() {
            if (updateMode != UpdateType.FixedUpdate || Time.deltaTime == 0f) {
                return;
            }
            RecalculateAcceleration(Time.deltaTime);
            Process(Time.deltaTime);
            foreach (VirtualBone b in bones) {
                b.Projection(this);
            }
        }
        public void Update() {
            if (updateMode != UpdateType.Update || Time.deltaTime == 0f) {
                return;
            }
            RecalculateAcceleration(Time.deltaTime);
            float timeToPass = Mathf.Min(Time.deltaTime, Time.maximumDeltaTime);
            while (timeToPass > Time.fixedDeltaTime) {
                Process(Time.fixedDeltaTime);
                timeToPass -= Time.fixedDeltaTime;
            }
            if (timeToPass > 0f) {
                Process(timeToPass);
            }
            foreach (VirtualBone b in bones) {
                b.Projection(this);
            }
        }
        //public void PrepareForChange() {
            //previousFrameBones = new List<VirtualBone>();
            //BuildVirtualBoneTree(previousFrameBones, root, root, depth);
        //}
        //public void ApplyChanges() {
            //List<VirtualBone> currentTree = new List<VirtualBone>();
            //BuildVirtualBoneTree(currentTree, root, root, depth);
    //
            //for (int i = 0; i < bones.Count; i++) {
                //bones[i].localStartPos += currentTree[i].localStartPos - previousFrameBones[i].localStartPos;
                //bones[i].localStartRot = (Quaternion.Inverse(previousFrameBones[i].localStartRot) * currentTree[i].localStartRot) * bones[i].localStartRot;
            //}
        //}
        public VirtualBone GetVirtualBone(Transform t) {
            foreach( VirtualBone b in bones) {
                if (b.self == t) {
                    return b;
                }
            }
            return null;
        }
        public bool IsSimulatingBone(Transform t) {
            if (bones == null) {
                Regenerate();
            }
            foreach( VirtualBone b in bones) {
                if (b.self == t) {
                    return true;
                }
            }
            return false;
        }
    }
}