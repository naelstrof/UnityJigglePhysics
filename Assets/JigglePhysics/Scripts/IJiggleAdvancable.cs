namespace JigglePhysics {
[System.Serializable]
public enum JiggleUpdateMode {
    LateUpdate,
    FixedUpdate
}
internal interface IJiggleAdvancable {
    public void Advance(float dt);
}

}
