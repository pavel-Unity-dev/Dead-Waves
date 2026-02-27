using UnityEngine;

public class ControlsEditor : MonoBehaviour
{
    public DraggableUI fireButton;
    public DraggableUI jumpButton;

    public void ToggleEditMode()
    {
        bool newState = !fireButton.editMode;

        fireButton.editMode = newState;
        jumpButton.editMode = newState;
    }

    public void SaveAll()
    {
        fireButton.SavePosition();
        jumpButton.SavePosition();
    }
}