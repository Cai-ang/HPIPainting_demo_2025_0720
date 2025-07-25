using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DrawonObj
{
    public class drawonobj : MonoBehaviour
    {
        public GameObject brushCursor, brushContainer; //The cursor that overlaps the model and our container for the brushes painted
        public Camera sceneCamera, canvasCam;  //The camera that looks at the model, and the camera that looks at the canvas.
        public Sprite cursorPaint, cursorDecal; // Cursor for the differen functions 
        public RenderTexture canvasTexture; // Render Texture that looks at our Base Texture and the painted brushes
        public Material baseMaterial; // The material of our base texture (Were we will save the painted texture)

        Painter_BrushMode mode; //Our painter mode (Paint brushes or decals)
        float brushSize = 1.0f; //The size of our brush
        Color brushColor; //The selected color
        int brushCounter = 0, MAX_BRUSH_COUNT = 1000; //To avoid having millions of brushes
        bool saving = false; //Flag to check if we are saving the texture


        void Update()
        {
            brushColor = ColorSelector.GetColor();  //Updates our painted color with the selected color
            if (Input.GetMouseButton(0))
            {
                DoAction();
            }
            //UpdateBrushCursor();
        }

        //The main action, instantiates a brush or decal entity at the clicked position on the UV map
        void DoAction()
        {
            if (saving)
                return;
            Vector3 uvWorldPosition = Vector3.zero;
            if (HitTestUVPosition(ref uvWorldPosition))
            {
                GameObject brushObj;

                brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity")); //Paint a brush
                brushObj.GetComponent<SpriteRenderer>().color = brushColor; //Set the brush color

                brushColor.a = brushSize * 2.0f; // Brushes have alpha to have a merging effect when painted over.
                brushObj.transform.parent = brushContainer.transform; //Add the brush to our container to be wiped later
                brushObj.transform.localPosition = uvWorldPosition; //The position of the brush (in the UVMap)
                brushObj.transform.localScale = Vector3.one * brushSize;//The size of the brush
            }
            brushCounter++; //Add to the max brushes
            if (brushCounter >= MAX_BRUSH_COUNT)
            { //If we reach the max brushes available, flatten the texture and clear the brushes
                brushCursor.SetActive(false);
                saving = true;
                Invoke("SaveTexture", 0.1f);

            }
        }
        //To update at realtime the painting cursor on the mesh
        void UpdateBrushCursor(Vector3 worldPosition)
        {
            if (!saving)
            {
                brushCursor.SetActive(true);
                brushCursor.transform.position = worldPosition;
            }
            else
            {
                brushCursor.SetActive(false);
            }
        }
        void DoPaint(RaycastHit hit)
        {
            Vector2 pixelUV = hit.textureCoord;
            Vector3 uvWorldPosition = new Vector3(pixelUV.x - canvasCam.orthographicSize,
                                                  pixelUV.y - canvasCam.orthographicSize,
                                                  0);

            GameObject brushObj;
            if (mode == Painter_BrushMode.PAINT)
            {
                brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/BrushEntity"));
                brushObj.GetComponent<SpriteRenderer>().color = brushColor;
            }
            else
            {
                brushObj = (GameObject)Instantiate(Resources.Load("TexturePainter-Instances/DecalEntity"));
            }

            brushColor.a = brushSize * 2.0f;
            brushObj.transform.parent = brushContainer.transform;
            brushObj.transform.localPosition = uvWorldPosition;
            brushObj.transform.localScale = Vector3.one * brushSize;

            brushCounter++;
            if (brushCounter >= MAX_BRUSH_COUNT)
            {
                SaveTexture();
            }
        }
        //Returns the position on the texuremap according to a hit in the mesh collider
        bool HitTestUVPosition(ref Vector3 uvWorldPosition)
        {
            RaycastHit hit;
            Vector3 cursorPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f);
            Ray cursorRay = sceneCamera.ScreenPointToRay(cursorPos);
            if (Physics.Raycast(cursorRay, out hit, 200))
            {
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (meshCollider == null || meshCollider.sharedMesh == null)
                    return false;
                Vector2 pixelUV = new Vector2(hit.textureCoord.x, hit.textureCoord.y);
                uvWorldPosition.x = pixelUV.x - canvasCam.orthographicSize;//To center the UV on X
                uvWorldPosition.y = pixelUV.y - canvasCam.orthographicSize;//To center the UV on Y
                uvWorldPosition.z = 0.0f;
                return true;
            }
            else
            {
                return false;
            }

        }
        //Sets the base material with a our canvas texture, then removes all our brushes
        void SaveTexture()
        {
            brushCounter = 0;
            System.DateTime date = System.DateTime.Now;
            RenderTexture.active = canvasTexture;
            Texture2D tex = new Texture2D(canvasTexture.width, canvasTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, canvasTexture.width, canvasTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            baseMaterial.mainTexture = tex; //Put the painted texture as the base
            foreach (Transform child in brushContainer.transform)
            {//Clear brushes
                Destroy(child.gameObject);
            }
            //StartCoroutine ("SaveTextureToFile"); //Do you want to save the texture? This is your method!
            Invoke("ShowCursor", 0.1f);
        }
        //Show again the user cursor (To avoid saving it to the texture)
        void ShowCursor()
        {
            saving = false;
        }

        ////////////////// PUBLIC METHODS //////////////////

        public void SetBrushMode(Painter_BrushMode brushMode)
        { //Sets if we are painting or placing decals
            mode = brushMode;
            brushCursor.GetComponent<SpriteRenderer>().sprite = brushMode == Painter_BrushMode.PAINT ? cursorPaint : cursorDecal;
        }
        public void SetBrushSize(float newBrushSize)
        { //Sets the size of the cursor brush or decal
            brushSize = newBrushSize;
            brushCursor.transform.localScale = Vector3.one * brushSize;
        }

    }
}