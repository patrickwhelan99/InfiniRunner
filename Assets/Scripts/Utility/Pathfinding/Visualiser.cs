using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Paz.Utility.PathFinding
{
    public class Visualiser
    {
        protected GameObject displayPrefab;
        protected GameObject displayObject;
        protected Texture2D displayTexture;
        protected Material materialToUpdate;

        protected Queue<(Vector2Int, Color)> playbackQueue = new Queue<(Vector2Int, Color)>();

        protected Texture2D oldTexture;

        protected int gridWidth;

        public Visualiser(int GridWidth = 0)
        {
            gridWidth = GridWidth;

            displayPrefab ??= Resources.Load("Prefabs/DisplaySearchPath") as GameObject;
            displayObject ??= Object.Instantiate(displayPrefab);
            materialToUpdate = displayObject.GetComponent<Renderer>().material;

            if (Camera.main != null)
            {
                displayObject.transform.position = Camera.main.transform.position + new Vector3(0.0f, -1.0f, 0.0f);
                Camera.main.orthographic = true;
            }
        }

        public void SetInstructions(IEnumerable<(Vector2Int, Color)> Instructions)
        {
            playbackQueue = new Queue<(Vector2Int, Color)>(Instructions);
        }

        public void EnqueueInstruction((Vector2Int, Color) Instruction)
        {
            playbackQueue.Enqueue(Instruction);
        }

        public void Playback(IEnumerable<Node> AllNodes = null, float DeltaTime = 0.001f)
        {
            Object.FindObjectOfType<GameController>().StartCoroutine(PlaybackAsync(AllNodes, DeltaTime));
        }

        protected virtual void SetupPlayback(IEnumerable<Node> AllNodes)
        {
            // Set initial Texture
            displayTexture = GetFreshTexture(gridWidth);

            oldTexture = GetFreshTexture(gridWidth);
        }

        protected IEnumerator PlaybackAsync(IEnumerable<Node> AllNodes, float DeltaTime)
        {
            SetupPlayback(AllNodes);
            (Vector2Int, Color) CurrentInstruction;

            while (playbackQueue.Count > 0)
            {
                CurrentInstruction = playbackQueue.Dequeue();

                RenderFrame(CurrentInstruction, oldTexture, gridWidth);

                Graphics.CopyTexture(displayTexture, oldTexture);

                yield return new WaitForSeconds(DeltaTime);
            }
        }

        protected void RenderFrame((Vector2Int Coord, Color Colour) Instruction, Texture2D OldTexture, int Size)
        {
            displayTexture = GetFreshTexture(Size);

            // Copy the texture from last frame
            Graphics.CopyTexture(OldTexture, displayTexture);

            // X is flipped
            displayTexture.SetPixel(Size - 1 - Instruction.Coord.x, Instruction.Coord.y, Instruction.Colour);
            displayTexture.Apply(true, true);

            materialToUpdate.mainTexture = displayTexture;
        }

        protected Texture2D GetFreshTexture(int Size)
        {
            Texture2D Ret = new Texture2D(Size, Size, TextureFormat.RGB24, 2, true)
            {
                anisoLevel = 0,
                filterMode = 0
            };

            return Ret;
        }

        public void Update(Node CurrentNode, Node[] AllNodes, HashSet<Node> OpenSet, Node[] Neighbours)
        {
            int Size = (int)Mathf.Sqrt(AllNodes.Length);

            displayTexture = new Texture2D(Size, Size, TextureFormat.RGB24, 2, true);

            for (int i = 0; i < AllNodes.Length; i++)
            {
                Node ThisNode = AllNodes[i];

                Color PixelColour = Color.white;
                if (ThisNode.Equals(CurrentNode))
                {
                    PixelColour = Color.red;
                }
                else if (Neighbours.Contains(ThisNode))
                {
                    PixelColour = Color.green;
                }
                else if (OpenSet.Contains(ThisNode))
                {
                    PixelColour = Color.blue;
                }
                else if (ThisNode.isBlocker)
                {
                    PixelColour = Color.black;
                }

                displayTexture.SetPixel(Size - 1 - (i % Size), i / Size, PixelColour);
            }


            displayTexture.anisoLevel = 0;
            displayTexture.filterMode = 0;

            displayTexture.Apply(true, true);


            materialToUpdate.mainTexture = displayTexture;
        }

        public void SetPosition(Vector3 NewPos)
        {
            if (displayObject != null)
            {
                displayObject.transform.position = NewPos;
            }
        }
    }

    public class PathFindingVisualiser : Visualiser
    {
        public PathFindingVisualiser(int Width) : base(Width)
        {
            // gridWidth = Width;

            // displayPrefab ??= Resources.Load("Prefabs/DisplaySearchPath") as GameObject;
            // displayObject ??= MonoBehaviour.Instantiate(displayPrefab);
            // materialToUpdate = displayObject.GetComponent<Renderer>().material;

            // displayObject.transform.position = Camera.main.transform.position + new Vector3(0.0f, -1.0f, 0.0f);
            // Camera.main.orthographic = true;
        }

        // public void ObservedSetModified(CollectionModifiedEventData<Node> EventData)
        // {
        //     // Open Set
        //     if(EventData.collection == pathFinder.OpenSet)
        //     {
        //         if(EventData.added != default(Node))
        //         {
        //             playbackQueue.Enqueue((EventData.added, Color.magenta));
        //         }
        //         else
        //         {
        //             playbackQueue.Enqueue((EventData.removed, Color.white));
        //         }
        //     }
        // }

        protected override void SetupPlayback(IEnumerable<Node> AllNodes)
        {
            // Set initial Texture
            displayTexture = GetFreshTexture(gridWidth);

            oldTexture = GetFreshTexture(gridWidth);

            // Set all the path blockers on the oldTexture which will be copied to the displayTexture
            AllNodes.Where(x => x.isBlocker).ToList().ForEach(x =>
            {
                oldTexture.SetPixel(gridWidth - 1 - x.Coord.x, x.Coord.y, Color.black);
            });
        }
    }
}
