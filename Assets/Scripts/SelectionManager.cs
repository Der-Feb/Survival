using UnityEngine;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    public GameObject interaction_info_ui;
    TextMeshProUGUI interaction_text;

    [SerializeField]
    private CameraFocus focusScript;

    private void Start()
    {
        interaction_text = interaction_info_ui.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if(focusScript != null && !focusScript.isFocusing)
        {
            interaction_info_ui.SetActive(false);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit))
        {
            var selectionTransform = hit.transform;
            InteractableObject interactable = selectionTransform.GetComponent<InteractableObject>();

            if(interactable == null)
            {
                interaction_info_ui.SetActive(false);
                return;
            }

            float distanceHit = hit.distance;
            string itemName = selectionTransform.GetComponent<InteractableObject>().GetItemName();
            
            interaction_text.text = $"{itemName} " + 
                "\n" +
                $"<size=70%>({distanceHit:F2}m)</size>";

            interaction_info_ui.SetActive(true);
        }
    }
}
