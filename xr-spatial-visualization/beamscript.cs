# Unity beamscript Code developed in Visual Studio

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Wave.OpenXR.Toolkit.Samples;
using Wave.OpenXR.Toolkit.CompositionLayer.Passthrough;
using Wave.OpenXR.CompositionLayer;
public class beamscript : MonoBehaviour
{
 private string file_name = "beamdata_Final";
 Vector3 headPosition;
 public TMP_Text displaytext;
 public int number_of_beams;
 public bool grid;
 //private int width=5;
 // private float spacing= 0.5f;
 public GameObject spherePrefab; // Assign the sphere prefab in the Unity Inspector
 private Vector3 transmitterPosition;
 private bool transmitterPlaced = false; // To ensure only one sphere is created
 public GameObject BeamPrefab;
 private GameObject[] BeamClone = new GameObject[25000];
 private GameObject BeamClone1;
 private LineRenderer Beam;
 private LineRenderer square;
 private string[][][] Beamdata;
 private int Curr_RxNum;
 private int Prev_RxNum = -1;
 int[] maxBeamsPerReceiver; // Declare this array globally

 public int max_receivers = 0;
 public int max_beams = 0;
 private int numInteraction = 0; // To track the current interaction beam
 private int pressCount = 0; // Counter to track how many times the trigger is pressed
 private Vector3[] receiverPositions; // Changed List to Array
 private int totalDisplayedBeams = 0; // To count the total beams for the current interaction
level
 private int currentInteractionLevel = 0;
 private int totalInteractionLevels = 5;
 // Start is called before the first frame update
 void Start()
 {
 Debug.Log("Inside Start");
 CompositionLayerPassthroughAPI.CreatePlanarPassthrough(LayerType.Underlay);
 Curr_RxNum = 1;
 Debug.Log("Reading CSV File");
 ReadCSV();
 if (grid)
 {
 Debug.Log("Making Boundary");
 //boundary(width, spacing);
 }
 }
 void Update()
 {
 Debug.Log("Inside Update");
 headPosition =
FindObjectOfType<ViveHeadsetLocation>().GetCurrentHeadsetPosition(); // Getting the
headset location from the other script
 Curr_RxNum = Pos_Calc(headPosition, receiverPositions); // Get the nearest receiver
based on the headset position

 // 1st Functionality: Whenever the user moves, the nearest receiver's beams are displayed
 if (Curr_RxNum != Prev_RxNum)
 {
 // User moved to a new position, check the interaction level and render beams
accordingly
 DeleteBeam(); // Clear previous beams
 if (currentInteractionLevel == 0)
 {
 totalDisplayedBeams = 0;
 // Show all beams when the interaction level is 0
 RenderBeam(Curr_RxNum);
 // Update the VR headset display with the total number of displayed beams
 displaytext.text = "Total Beams:\n " + totalDisplayedBeams;
 }
 else
 {
 totalDisplayedBeams = 0;
 // Render beams according to the current interaction level
 for (int level = 0; level < currentInteractionLevel; level++)
 {
 CycleInteractionBeam(Curr_RxNum, level); // Render beams up to the current
interaction level
 }
 // Update the VR headset display with the total number of displayed beams
 displaytext.text = "Total Beams: \n" + totalDisplayedBeams;
 }
 Debug.Log("Rendering Beams after Position Change");
 Prev_RxNum = Curr_RxNum; // Update the previous receiver number to the current one
 }
 // 2nd Functionality: On each Left Trigger press, numofInteraction value changes and those
particular beams display for that receiver
 if (VRSInputManager.instance.GetButtonDown(VRSButtonReference.TriggerL))
 {
 Debug.Log("Left trigger pressed");

 // Increment the interaction level
 currentInteractionLevel++;
 // Ensure interaction level doesn't exceed the max levels
 if (currentInteractionLevel >= totalInteractionLevels)
 {
 currentInteractionLevel = 0; // Reset interaction level if it exceeds max
 }
 // Re-render beams for the current receiver and updated interaction level
 DeleteBeam(); // Clear previous beams
 if (currentInteractionLevel == 0)
 {
 RenderBeam(Curr_RxNum); // Show all beams if interaction level is 0
 // Update the VR headset display with the total number of displayed
beams
 displaytext.text = "Total Beams: \n" + totalDisplayedBeams;
 }
 else
 {
 totalDisplayedBeams = 0;
 // Show beams for the current interaction level and lower levels
 for (int level = 0; level < currentInteractionLevel; level++)
 {
 CycleInteractionBeam(Curr_RxNum, level); // Render beams for all interaction
levels up to current
 }
 // Update the VR headset display with the total number of displayed beams
 displaytext.text = "Total Beams: \n" + totalDisplayedBeams;
 }
 }
 Debug.Log("Ended Update");
 }
 int Pos_Calc(Vector3 userPosition, Vector3[] receiverPositions)
 {

 if (receiverPositions == null || receiverPositions.Length == 0)
 {
 Debug.LogError("receiverPositions is not initialized or empty.");
 return -1; // Return a default value
 }
 int nearestReceiverIndex = -1; // Default to -1 if no receiver is found
 float minDistance = float.MaxValue; // Start with the maximum possible distance
 // Loop through all receiver positions
 for (int i = 0; i < receiverPositions.Length; i++)
 {
 if (receiverPositions[i] == Vector3.zero) continue; // Skip uninitialized positions
 // Calculate the Euclidean distance between the user's position and the receiver's position
 float distance = Vector3.Distance(userPosition, receiverPositions[i]);
 // Update if this receiver is closer
 if (distance < minDistance)
 {
 minDistance = distance;
 nearestReceiverIndex = i + 1; // Assuming receiver numbers are 1-based
 }
 }
 Debug.Log($"Nearest Receiver: {nearestReceiverIndex}, Distance: {minDistance}");
 return nearestReceiverIndex;
 }
 void ReadCSV()
 {
 TextAsset txt = (TextAsset)Resources.Load(file_name, typeof(TextAsset)); // Ensure the CSV
file is in the \Resources folder.
 if (txt == null)
 {
 Debug.LogError(file_name + " could not be loaded");
 return;
 }

 Debug.Log("Reading CSV: " + file_name);
 // Parse the CSV file
 string[] data = txt.text.Split(new char[] { '\n' },
System.StringSplitOptions.RemoveEmptyEntries);
 int totnum = data.Length;
 // Initialize the receiverPositions array
 receiverPositions = new Vector3[max_receivers];
 // Step 1: Determine max_receivers and max_beams dynamically
 for (int i = 1; i < totnum; i++) // Skip header
 {
 string[] tempcords = data[i].Split(new char[] { ',' });
 int cur_rx = int.Parse(tempcords[0]);
 int cur_beam = int.Parse(tempcords[1]);
 if (cur_rx > max_receivers)
 max_receivers = cur_rx;
 if (cur_beam > max_beams)
 max_beams = cur_beam;
 // Capture transmitter position from the first row
 if (!transmitterPlaced)
 {
 string[] transmitterCoords = tempcords[3].Split(new char[] { ';' });
 float txX = float.Parse(transmitterCoords[0]);
 float txY = float.Parse(transmitterCoords[2]); // MATLAB Z -> Unity Y
 float txZ = -float.Parse(transmitterCoords[1]); // -MATLAB Y -> Unity Z
 transmitterPosition = new Vector3(txX, txY, txZ);
 PlaceTransmitterSphere(transmitterPosition);
 transmitterPlaced = true;
 }
 // Populate receiver positions (only for the first beam of each receiver)
 if (cur_rx <= max_receivers && receiverPositions[cur_rx - 1] == Vector3.zero)

 {
 string[] receiverCoords = tempcords[4].Split(new char[] { ';' });
 float rxX = float.Parse(receiverCoords[0]);
 float rxY = float.Parse(receiverCoords[2]); // MATLAB Z -> Unity Y
 float rxZ = -float.Parse(receiverCoords[1]); // -MATLAB Y -> Unity Z
 receiverPositions[cur_rx - 1] = new Vector3(rxX, rxY, rxZ);
 Debug.Log($"Receiver {cur_rx} position: {receiverPositions[cur_rx - 1]}");
 }
 Curr_RxNum = cur_rx;
 }
 Debug.Log($"Max Receivers: {max_receivers}, Max Beams: {max_beams}");
 // Step 2: Allocate memory for Beamdata and maxBeamsPerReceiver
 Beamdata = new string[max_receivers + 1][][];
 maxBeamsPerReceiver = new int[max_receivers + 1];
 for (int rx = 0; rx <= max_receivers; rx++)
 {
 Beamdata[rx] = new string[max_beams + 1][];
 maxBeamsPerReceiver[rx] = 0; // Initialize max beam count for each receiver
 }
 // Step 3: Populate Beamdata array and track max beams per receiver
 for (int i = 1; i < totnum; i++) // Skip header
 {
 string[] tempcords = data[i].Split(new char[] { ',' });
 int cur_rx = int.Parse(tempcords[0]);
 int cur_beam = int.Parse(tempcords[1]);
 // Store data in Beamdata
 Beamdata[cur_rx][cur_beam] = tempcords;
 // Update max beams for this receiver
 if (cur_beam > maxBeamsPerReceiver[cur_rx])
 maxBeamsPerReceiver[cur_rx] = cur_beam;

 }
 Debug.Log("CSV Read Complete");
 }
 void RenderBeam(int receiver)
 {
 totalDisplayedBeams = 0;
 Debug.Log("Inside Render for Receiver: " + receiver);
 if (Beamdata[receiver] == null)
 {
 Debug.Log("No data for Receiver: " + receiver);
 return;
 }
 int num_beams = maxBeamsPerReceiver[receiver]; // Use actual beam count
 Debug.Log("Total Beams for Receiver " + receiver + ": " + num_beams);
 for (int i = 1; i <= num_beams; i++)
 {
 string[] cords = Beamdata[receiver][i];
 if (cords == null || cords.Length == 0)
 {
 Debug.Log($"No data for Receiver: {receiver}, Beam: {i}");
 continue;
 }
 int numVertices = int.Parse(cords[2]) + 2;
 Vector3[] positions = new Vector3[numVertices];
 Debug.Log("Receiver: " + receiver + " Beam Number: " + i + " Reflections: " +
numVertices);
 for (int j = 0; j < numVertices; j++)
 {
 string[] points = cords[j + 3].Split(new char[] { ';' });
 float x = float.Parse(points[0]); // MATLAB X -> Unity X
 float y = float.Parse(points[2]); // MATLAB Z -> Unity Y
49
 float z = -float.Parse(points[1]); // -MATLAB Y -> Unity Z
 positions[j] = new Vector3(x, y, z);
 }
 BeamClone[i] = Instantiate(BeamPrefab, this.transform);
 Beam = BeamClone[i].GetComponent<LineRenderer>();
 Beam.positionCount = numVertices;
 float R = 1.0f - ((float)i / (float)num_beams);
 float B = ((float)i / (float)num_beams);
 Color customColour1 = new Color(R, 0.0f, B, 1.0f);
 Color customColour2 = new Color(R * 0.5f, 0.0f, B * 0.5f, 1.0f);
 Beam.material.color = customColour1;
 Beam.startColor = customColour1;
 Beam.endColor = customColour2;
 Beam.SetPositions(positions);
 totalDisplayedBeams++;
 }
 }
 void DeleteBeam()
 {
 foreach (GameObject obj in BeamClone)
 {
 Destroy(obj);
 }
 }
 void PlaceTransmitterSphere(Vector3 position)
 {
 GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity);
 Debug.Log("Transmitter placed at: " + position);
 }
 void CycleInteractionBeam(int receiver, int interactionLevel)

 {
 // Validate the receiver index
 if (receiver <= 0 || receiver > max_receivers)
 {
 Debug.LogWarning("Invalid receiver number for cycling interaction beams.");
 return;
 }
 // Check if beam data exists for the receiver
 if (Beamdata[receiver] == null || Beamdata[receiver].Length == 0)
 {
 Debug.LogWarning("No beam data available for receiver: " + receiver);
 return;
 }
 int num_beams = maxBeamsPerReceiver[receiver];
 // Check if the receiver has beams
 if (num_beams == 0)
 {
 Debug.LogWarning("No beams for this receiver.");
 return;
 }
 Debug.Log($"Rendering beams for Receiver {receiver}, interactionLevel
{interactionLevel}");
 // Loop through all beams and filter by the specified interaction level
 for (int beamIndex = 0; beamIndex < num_beams; beamIndex++)
 {
 string[] cords = Beamdata[receiver][beamIndex + 1]; // Access beam data
 if (cords == null || cords.Length < 3 || int.Parse(cords[2]) != interactionLevel)
 {
 continue; // Skip beams not matching the specified interaction level
 }
 int numVertices = cords.Length - 3;
 Vector3[] positions = new Vector3[numVertices];

 for (int j = 0; j < numVertices; j++)
 {
 string[] points = cords[j + 3].Split(new char[] { ';' });
 float x = float.Parse(points[0]); // MATLAB X -> Unity X
 float y = float.Parse(points[2]); // MATLAB Z -> Unity Y
 float z = -float.Parse(points[1]); // -MATLAB Y -> Unity Z
 positions[j] = new Vector3(x, y, z);
 }
 BeamClone[beamIndex] = Instantiate(BeamPrefab, this.transform);
 LineRenderer beam = BeamClone[beamIndex].GetComponent<LineRenderer>();
 beam.positionCount = numVertices;
 // Assign colors based on interaction level
 Color customColour1, customColour2;
 // Define explicit colors for each interaction level
 switch (interactionLevel)
 {
 case 0:
 customColour1 = new Color(1f, 0.48f, 0.54f); // No interaction: light coral pink
(#FF7B89)
 customColour2 = new Color(1f, 0.48f, 0.54f); // Same for customColour2
 break;
 case 1:
 customColour1 = new Color(0.35f, 0.62f, 0.41f); // One interaction: muted green
(#5A9F68)
 customColour2 = new Color(0.35f, 0.62f, 0.41f); // Same for customColour2
 break;
 case 2:
 customColour1 = new Color(0.57f, 0.43f, 0.84f); // Two interactions: lavender
purple (#916DD5)
 customColour2 = new Color(0.57f, 0.43f, 0.84f); // Same for customColour2
 break;
 case 3:

 customColour1 = new Color(0.46f, 0.56f, 0.72f); // Three interactions: soft blue
(#758EB7)
 customColour2 = new Color(0.46f, 0.56f, 0.72f); // Same for customColour2
 break;
 default:
 customColour1 = Color.blue; // Default: blue
 customColour2 = Color.blue; // Same for customColour2
 break;
 }
 beam.material.color = customColour1;
 beam.startColor = customColour1;
 beam.endColor = customColour2;
 beam.SetPositions(positions);
 // Count the beam as displayed
 totalDisplayedBeams++; }
 }
}