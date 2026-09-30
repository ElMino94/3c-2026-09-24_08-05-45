using UnityEngine;
using UnityEngine.Rendering;

// À mettre sur l'objet racine du modèle du personnage (celui qui a l'Animator).
// Rend tout le corps invisible à la caméra, tout en gardant son ombre au sol.
public class HideBodyFromCamera : MonoBehaviour
{
    private void Awake()
    {
        // On parcourt TOUS les Renderer, à n'importe quelle profondeur
        // (corps, cheveux, bottes, armure, arme...) et on les passe en "ombre uniquement".
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}