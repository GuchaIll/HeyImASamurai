using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Animator_Manager : MonoBehaviour
{
    Animator animator;
     int horizontal;
     int vertical;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if(animator == null)
            Debug.LogError("Animator component not found in children of " + gameObject.name);
        horizontal = Animator.StringToHash("Horizontal");
        vertical = Animator.StringToHash("Vertical");
    }

    // Update is called once per frame
    public void UpdateAnimatorValues(float horizontalMovement, float verticalMovement)
    {
        //Animation snapping 
        float snappedHorizontal;
        float snappedVertical;
        if (horizontalMovement > 0 && horizontalMovement < 0.55f)
        {
            snappedHorizontal = 0.5f;
        }
        else if (horizontalMovement >= 0.55f)
        {
            snappedHorizontal = 1;
        }
        else if (horizontalMovement < 0 && horizontalMovement > -0.55f)
        {
            snappedHorizontal = -0.5f;
        }
        else if (horizontalMovement <= -0.55f)
        {
            snappedHorizontal = -1;
        }
        else
        {
            snappedHorizontal = 0;
        }
        if (verticalMovement > 0 && verticalMovement < 0.55f)
        {
            snappedVertical = 0.5f;
        }
        else if (verticalMovement >= 0.55f)
        {
            snappedVertical = 1;
        }
        else if (verticalMovement < 0 && verticalMovement > -0.55f)
        {
            snappedVertical = -0.5f;
        }
        else if (verticalMovement <= -0.55f)
        {
            snappedVertical = -1;
        }
        else
        {
            snappedVertical = 0;
        }
        animator.SetFloat(this.horizontal, snappedHorizontal, 0.1f, Time.deltaTime);
        animator.SetFloat(this.vertical, snappedVertical, 0.1f, Time.deltaTime);
        
    }
}
