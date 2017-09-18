using UnityEngine;
using System.Collections;

public interface IState
{
    void on_enter();
    void on_exit();
}
