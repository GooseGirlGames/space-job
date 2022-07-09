using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;
using System.Collections.Generic;
using Random=UnityEngine.Random;


public class FireInstance : RangedInteractableBase
{
    private float m_DamagePerTick;
    private DeathZone m_DeathField;
    private Transform m_SpawnLocation;
    private Room m_Room;
    private bool m_IsActive;
    private float m_MaxSize = 1.8f;
    private float m_size;
    private bool m_CanJump = false;
    private Vector3 m_InitialSize;
    private Coroutine m_GrowCoroutine;
    private Coroutine m_JumpCoroutine;
    //public List<FireInstanceSpawnLocation> NeighbourFireLocations;

    public Room GetRoom(){
        return m_Room;
    }

    public override void Start() {
        base.Start();
        m_DeathField = GetComponentInChildren<DeathZone>();
        m_InitialSize = transform.localScale;
        m_DeathField.SetDamage(0.005f);
    }

    protected override void Interaction(){
        if (PlayerAvatar.IsHolding<FireExtinguisher>()) {
            ResolvedServerRpc();
        }
    }

    public override string FriendlyName() {
        return "Fire";
    }

    public void Setup(Room room, Transform spawn) {
        m_Room = room;
        name = "Fire (" + room.name + ")";
        m_SpawnLocation = spawn;
        if (IsServer) {
            m_GrowCoroutine = StartCoroutine(Grow());
            m_JumpCoroutine = StartCoroutine(Jump());
        }
    }

    private IEnumerator Grow() {
        while(true){
            if(transform.localScale.x <= 0.15f){
                m_CanJump = false;
                Debug.Log("FireDie");
                ResolvedServerRpc();
            }
            else if(m_Room.RoomOxygen <= 0.5f){
                yield return new WaitForSeconds(1f);
                GrowFireClientRpc(0.1f);
                m_CanJump = false;
            }
            else if(transform.localScale.x < m_MaxSize){
                yield return new WaitForSeconds(Random.Range(0.5f, 2f));
                GrowFireClientRpc(1.05f);
                if(transform.localScale.x >= m_MaxSize-0.1f){
                    
                    m_CanJump = true;
                }
            }
            yield return new WaitForEndOfFrame();
            //yield return new WaitForSeconds(0.5f);
        }
        //Debug.Log("Grown to compleation");
    }
    private IEnumerator Jump() {
        while(true){
            yield return new WaitForSeconds(1f);
            if(m_CanJump){
                List<Transform> Neighbours = new List<Transform>();
                foreach(Transform spawn in m_Room.m_FireSpawnLocations){
                    float distance = (transform.position - spawn.position).magnitude;
                    if(distance <= 2f * m_size){
                        //Debug.Log("distance: " + distance + ", Scale:" + m_size);
                        Neighbours.Add(spawn);
                    }
                }
                //yield return new WaitForSeconds(1f + Random.Range(0f, 2f));
                yield return new WaitForSeconds(5f + Random.Range(0f, 30f));
                if(m_CanJump){
                    m_Room.FireJumpOver(Neighbours);
                }
            }
        }
    }


    [ServerRpc(RequireOwnership=false)]
    public void ResolvedServerRpc() {
        Debug.Log("Fire extinguished in " + m_Room);
        this.StopAllCoroutines();
        m_Room.FireResolved(this, m_SpawnLocation);
        GetComponent<NetworkObject>().Despawn(destroy: true);  // breach do be gone
    }

    [ClientRpc]
    public void GrowFireClientRpc(float scaling_factor) {
        transform.localScale = scaling_factor * transform.localScale;
        m_size = transform.localScale.x;
    }
}
