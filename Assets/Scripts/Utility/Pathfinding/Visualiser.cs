using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Paz.Utility.Collections;

namespace Paz.Utility.PathFinding
{
    public class Visualiser
    {
        private AStar pathFinder;
        private static GameObject displayPrefab;
        private static GameObject displayObject;
        Texture2D displayTexture;
        Material materialToUpdate;

        Queue<(Vector2Int, Color)> playbackQueue = new Queue<(Vector2Int, Color)>();

        public Visualiser(AStar PathFinder)
        {
            pathFinder = PathFinder;

            displayPrefab ??= Resources.Load("Prefabs/DisplaySearchPath") as GameObject;
            displayObject ??= MonoBehaviour.Instantiate(displayPrefab);
            materialToUpdate = displayObject.GetComponent<Renderer>().material;

            displayObject.transform.position = Camera.main.transform.position + new Vector3(0.0f, -1.0f, 0.0f);
            Camera.main.orthographic = true;
        }

        public void ObservedSetModified(CollectionModifiedEventData<Node> EventData)
        {
            // Open Set
            if(EventData.collection == pathFinder.OpenSet)
            {
                if(EventData.added != default(Node))
                {
                    playbackQueue.Enqueue((EventData.added, Color.magenta));
                }
                else
                {
                    playbackQueue.Enqueue((EventData.removed, Color.white));
                }
            }
        }

        public void EnqueueInstruction((Vector2Int, Color) Instruction)
        {
            playbackQueue.Enqueue(Instruction);
        }

        public void Playback()
        {
            MonoBehaviour.FindObjectOfType<GameController>().StartCoroutine(PlaybackAsync());
        }

        private IEnumerator PlaybackAsync()
        {
            (Vector2Int, Color) CurrentInstruction;

            // Set initial Texture
            int Size = pathFinder.width;
            displayTexture = GetFreshTexture(Size);

            Texture2D OldTexture = GetFreshTexture(Size);

            // Set all the path blockers on the oldTexture which will be copied to the displayTexture
            pathFinder.AllNodes.Where(x => x.isBlocker).ToList().ForEach(x =>
            {
                OldTexture.SetPixel(Size - 1 - x.Coord.x, x.Coord.y, Color.black);
            });

            while(playbackQueue.Count > 0)
            {
                CurrentInstruction = playbackQueue.Dequeue();

                RenderFrame(CurrentInstruction, OldTexture);

                Graphics.CopyTexture(displayTexture, OldTexture);

                yield return new WaitForSeconds(0.001f);
            }
        }

        private void RenderFrame((Vector2Int Coord, Color Colour) Instruction, Texture2D OldTexture)
        {
            int Size = pathFinder.width;
            displayTexture = GetFreshTexture(Size);

            // Copy the texture from last frame
            Graphics.CopyTexture(OldTexture, displayTexture);

            // X is flipped
            displayTexture.SetPixel(Size - 1 - Instruction.Coord.x, Instruction.Coord.y, Instruction.Colour);
            displayTexture.Apply(true, true);

            materialToUpdate.mainTexture = displayTexture;
        }

        private Texture2D GetFreshTexture(int Size)
        {
            Texture2D Ret = new Texture2D(Size, Size, TextureFormat.RGB24, 2, true);
            Ret.anisoLevel = 0;
            Ret.filterMode = 0;

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
                if(ThisNode == CurrentNode)
                {
                    PixelColour = Color.red;
                }
                else if(Neighbours.Contains(ThisNode))
                {
                    PixelColour = Color.green;
                }
                else if(OpenSet.Contains(ThisNode))
                {
                    PixelColour = Color.blue;
                }
                else if(ThisNode.isBlocker)
                {
                    PixelColour = Color.black;
                }

                displayTexture.SetPixel(Size - 1 - i % Size, i / Size, PixelColour);
            }


            displayTexture.anisoLevel = 0;
            displayTexture.filterMode = 0;

            displayTexture.Apply(true, true);


            materialToUpdate.mainTexture = displayTexture;
        }
    }
}
