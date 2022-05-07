using Unity.Netcode;

public abstract class Interactable<T> : InteractableBase where T : unmanaged {

    protected NetworkVariable<T> m_State = new NetworkVariable<T>();

    public override void OnNetworkSpawn(){
        m_State.OnValueChanged += OnStateChange;
    }
    public override void OnNetworkDespawn(){
        m_State.OnValueChanged -= OnStateChange;
    }

    public abstract void OnStateChange(T previous, T current);

    public T Value {
        get => m_State.Value;
    }

    public override void Update() {
        base.Update();
    }

}
