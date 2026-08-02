using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Virus object.
/// </summary>
public class CVirus : MonoBehaviour {

    // Map position.
    public short cell { get; private set; }

    GameObject appear;
    GameObject disappear;


    void Awake()
    {
        // Object to use when created.
        this.appear = transform.FindChild("appear").gameObject;
        this.appear.SetActive(false);

        // Object to use when disappearing.
        this.disappear = transform.FindChild("destroy").gameObject;
        this.disappear.SetActive(false);
    }


    public void update_position(short cell)
    {
        this.cell = cell;
    }


    /// <summary>
    /// Sets the idle state.
    /// </summary>
    public void idle()
    {
        // Make it untouchable.
        GetComponent<BoxCollider>().enabled = false;

        this.appear.SetActive(true);
        // Stop the animation.
        this.appear.GetComponent<CRotator>().stop();
    }


    /// <summary>
    /// Makes it touchable.
    /// </summary>
    public void touchable()
    {
        GetComponent<BoxCollider>().enabled = true;
    }


    /// <summary>
    /// Deletes this virus.
    /// </summary>
    public void destroy()
    {
        this.appear.SetActive(false);
        this.disappear.SetActive(true);
    }


    public void on_touch()
    {
        // Play swaying left and right animation.
        this.appear.GetComponent<CRotator>().play();
    }


    public bool is_same(short cell)
    {
        return this.cell == cell;
    }
}
