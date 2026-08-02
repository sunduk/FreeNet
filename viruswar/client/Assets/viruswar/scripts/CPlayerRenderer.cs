using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for rendering the player's current state.
/// </summary>
public class CPlayerRenderer : MonoBehaviour {

    CPlayer owner;
    GameObject prefab_character;
    List<CVirus> viruses;


    void Awake()
    {
        this.viruses = new List<CVirus>();
    }


    public void initialize(CPlayer owner)
    {
        this.owner = owner;

        switch (this.owner.player_index)
        {
            case 0:
                this.prefab_character = Resources.Load("prefabs/red") as GameObject;
                break;

            case 1:
                this.prefab_character = Resources.Load("prefabs/blue") as GameObject;
                break;
        }
    }


    public void clear()
    {
        foreach (var virus in this.viruses)
        {
            GameObject.Destroy(virus.gameObject);
        }
        this.viruses.Clear();
    }


    public void add(short position)
    {
        // Create an instance.
        GameObject clone = CGameWorld.Instance.instantiate(this.prefab_character);
        clone.transform.parent = transform;

        // Set position.
        Vector2 map_position = CHelper.convert_to_position(position);
        clone.transform.localPosition = CHelper.map_to_world(map_position);

        // Set default state.
        CVirus virus = clone.GetComponent<CVirus>();
        virus.update_position(position);
        virus.idle();

        this.viruses.Add(virus);
    }


    public void remove(short position)
    {
        CVirus virus = this.viruses.Find(v => v.is_same(position));
        if (virus == null)
        {
            // Should not be null
            Debug.LogErrorFormat("Cannot find a virus of the position. position : {0}", position);
            return;
        }

        this.viruses.Remove(virus);
        virus.destroy();
        GameObject.Destroy(virus.gameObject, 1.0f);
    }


    /// <summary>
    /// Makes all viruses touchable.
    /// </summary>
    public void ready()
    {
        foreach (var virus in this.viruses)
        {
            virus.touchable();
        }
    }


    /// <summary>
    /// Makes all viruses untouchable.
    /// </summary>
    public void idle()
    {
        foreach (var virus in this.viruses)
        {
            virus.idle();
        }
    }


    public void stop()
    {
        foreach (var virus in this.viruses)
        {
            virus.transform.FindChild("appear").GetComponent<CRotator>().stop();
        }
    }
}
