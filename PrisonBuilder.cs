using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class PrisonBuilder : MonoBehaviour
{
    [SerializeField] private int minAmoutOfRoomsToCreate = 2;
    [SerializeField] private int sizeOfFloor = 10;

    //En lista med prefabs med alla rum
    [SerializeField] private List<GameObject> allAvalibleRooms = new List<GameObject>();
    [SerializeField] private List<GameObject> endRooms = new List<GameObject>();
    [SerializeField] private bool debug = false;

    private Queue<GameObject> leftToProcessRooms = new Queue<GameObject>();

    //Griddet #map
    private Dictionary<Vector3, Room> Grid = new Dictionary<Vector3, Room>();
    private Dictionary<DoorDir, Vector3> directionModifyer = new Dictionary<DoorDir, Vector3>();

    public Vector3 spawnPosition;
    int currentAmountOfRooms = 0;
    bool initialized = false;

    public void Initialize()
    {
        if (initialized == true) return;

        directionModifyer.Add(DoorDir.NORTH, new Vector3(0, 0, 1));
        directionModifyer.Add(DoorDir.EAST, new Vector3(1, 0, 0));
        directionModifyer.Add(DoorDir.SOUTH, new Vector3(0, 0, -1));
        directionModifyer.Add(DoorDir.WEST, new Vector3(-1, 0, 0));
        initialized = true;
    }

    public void SpawnWorld(int roomAmount)
    {
        minAmoutOfRoomsToCreate = roomAmount;
        Initialize();
        BuildPrison();
        SumUpPrison();
    }

    //BuildPrison
    public void BuildPrison()
    {
        Vector3 roomPos = Vector3.zero;
        int random = Random.Range(0, allAvalibleRooms.Count);
        Room room = Instantiate(allAvalibleRooms[random], roomPos * sizeOfFloor,
                allAvalibleRooms[random].transform.rotation)
            .GetComponent<Room>();

        room.ownPos = roomPos;
        leftToProcessRooms.Enqueue(room.gameObject);
        Grid.Add(roomPos, room);

        if (debug == false)
        {
            do
            {
                BuildNeighboringRoom();
            } while (minAmoutOfRoomsToCreate > currentAmountOfRooms);
        }
    }

    private void BuildNeighboringRoom()
    {
        if (leftToProcessRooms.Count == 0) return;
        
        Room currentRoom = leftToProcessRooms.Dequeue().GetComponent<Room>();
        Vector3 roomPos = currentRoom.ownPos;

        foreach (var dire in currentRoom.GetDoorDir())
        {
            switch (dire)
            {
                case DoorDir.SOUTH:
                    // Look up
                    CreateRoom(roomPos, DoorDir.SOUTH, DoorDir.NORTH, DoorDir.EAST, DoorDir.WEST);
                    break;
                case DoorDir.EAST:
                    CreateRoom(roomPos, DoorDir.EAST, DoorDir.WEST, DoorDir.SOUTH, DoorDir.NORTH);
                    break;
                case DoorDir.WEST:
                    CreateRoom(roomPos, DoorDir.WEST, DoorDir.EAST, DoorDir.SOUTH, DoorDir.NORTH);
                    break;
                case DoorDir.NORTH:
                    CreateRoom(roomPos, DoorDir.NORTH, DoorDir.SOUTH, DoorDir.EAST, DoorDir.WEST);
                    break;
            }
        }
    }

    public void OnBuildNeighborRoom()
    {
        if (debug == true)
        {
            if (minAmoutOfRoomsToCreate > currentAmountOfRooms)
            {
                Debug.Log("Building neighbor room");
                BuildNeighboringRoom();
            }
            else
            {
                Debug.Log("Building closing room");
                BuildClosingRoom();
            }
        }
    }

    private void SumUpPrison()
    {
        foreach (var roomObj in leftToProcessRooms)
        {
            Room room = roomObj.GetComponent<Room>();
            CloseRoomInAllDirections(room);
        }
    }

    private void BuildClosingRoom()
    {
        if (leftToProcessRooms.Count == 0) return;

        Room room = leftToProcessRooms.Dequeue().GetComponent<Room>();
        CloseRoomInAllDirections(room);
    }

    private void CloseRoomInAllDirections(Room room)
    {
        foreach (var dire in room.GetDoorDir())
        {
            switch (dire)
            {
                case DoorDir.SOUTH:
                    // Look up
                    CloseRoom(room.ownPos, DoorDir.SOUTH, DoorDir.NORTH, DoorDir.EAST, DoorDir.WEST);
                    break;
                case DoorDir.EAST:
                    CloseRoom(room.ownPos, DoorDir.EAST, DoorDir.WEST, DoorDir.SOUTH, DoorDir.NORTH);
                    break;
                case DoorDir.WEST:
                    CloseRoom(room.ownPos, DoorDir.WEST, DoorDir.EAST, DoorDir.SOUTH, DoorDir.NORTH);
                    break;
                case DoorDir.NORTH:
                    CloseRoom(room.ownPos, DoorDir.NORTH, DoorDir.SOUTH, DoorDir.EAST, DoorDir.WEST);
                    break;
            }
        }
    }

    private void CloseRoom(Vector3 roomPos, DoorDir doorDir, DoorDir reversed, DoorDir remaningOne, DoorDir remaningTwo)
    {
        PickCorrectRoomToSpawn(roomPos, doorDir, reversed, remaningOne, remaningTwo);
        if (openingstatusMap.Count > 0)
        {
            GameObject roomObj = ChooseClosingRoom();

            if (roomObj != null)

            {
                Room room = Instantiate(roomObj, (roomPos + directionModifyer[doorDir]) * sizeOfFloor,
                    roomObj.transform.rotation).GetComponent<Room>();

                room.ownPos = roomPos + directionModifyer[doorDir];
                Grid.Add(room.ownPos, room);
            }
        }
    }

    private GameObject ChooseClosingRoom()
    {
        var neededDirections = NeededDirections();
        List<GameObject> roomPrefabs = new List<GameObject>();
        List<GameObject> allRooms = allAvalibleRooms.Concat(endRooms).ToList();
        foreach (var roomGameObj in allRooms)
        {
            if (neededDirections.All(i => roomGameObj.GetComponent<Room>().GetDoorDir().Contains(i)) &&
                roomGameObj.GetComponent<Room>().GetDoorDir().Count == neededDirections.Count)
            {
                roomPrefabs.Add(roomGameObj);
            }
        }

        return roomPrefabs[Random.Range(0, roomPrefabs.Count)];
    }

    private void CreateRoom(Vector3 roomPos, DoorDir doorDir, DoorDir reversed, DoorDir remaningOne,
        DoorDir remaningTwo)
    {
        PickCorrectRoomToSpawn(roomPos, doorDir, reversed, remaningOne, remaningTwo);
        if (openingstatusMap.Count <= 0) return;
        GameObject roomObject = chooseRoom();
        if (roomObject == null) return;


        Room room = Instantiate(roomObject, (roomPos + directionModifyer[doorDir]) * sizeOfFloor,
            roomObject.transform.rotation).GetComponent<Room>();

        room.ownPos = roomPos + directionModifyer[doorDir];
        Grid.Add(room.ownPos, room);
        leftToProcessRooms.Enqueue(room.gameObject);
        room.doorOpenings = openingstatusMap; // DEBUG VARIBLE
        currentAmountOfRooms++;
    }


    private GameObject chooseRoom()
    {
        var neededDirections = NeededDirections();


        List<GameObject> theRoomsWithCorrectDirections = new List<GameObject>();
        List<GameObject> roomWithNoFreeOpenings = new List<GameObject>();
        foreach (var roomPrefab in allAvalibleRooms)
        {
            // Här ska regler skrivas in för vilket rum vi ska använda.

            Room room = roomPrefab.GetComponent<Room>();
            if (neededDirections.All(i => room.GetDoorDir().Contains(i))
                && room.GetDoorDir()
                    .All(i => openingstatusMap[i] !=
                              doorOpeningstatus.CLOSED) // Finns det någon i prefaben som är öppen mot en closed.(TA ej)
               )
            {
                if (room.GetDoorDir().Any(i => openingstatusMap[i] == doorOpeningstatus.FREE))
                {
                    theRoomsWithCorrectDirections.Add(roomPrefab);
                }
                else
                {
                    roomWithNoFreeOpenings.Add(roomPrefab);
                }
            }
        }

        if (roomWithNoFreeOpenings.Count == 0 && theRoomsWithCorrectDirections.Count == 0)
        {
            foreach (var endRoom in endRooms)
            {
                if (!endRoom.GetComponent<Room>().GetDoorDir().Contains(neededDirections[0])) continue;

                theRoomsWithCorrectDirections.Add(endRoom);
                break;
            }
        }

        return theRoomsWithCorrectDirections.Count != 0
            ? theRoomsWithCorrectDirections[Random.Range(0, theRoomsWithCorrectDirections.Count)]
            : roomWithNoFreeOpenings[Random.Range(0, roomWithNoFreeOpenings.Count)];
    }

    private List<DoorDir> NeededDirections()
    {
        List<DoorDir> neededDirections = new List<DoorDir>();
        if (openingstatusMap[DoorDir.NORTH] is doorOpeningstatus.OPEN) // OM NORTH ÄR ÖPPEN LÄgg till den som needed
        {
            neededDirections.Add(DoorDir.NORTH);
        }

        if (openingstatusMap[DoorDir.EAST] is doorOpeningstatus.OPEN)
        {
            neededDirections.Add(DoorDir.EAST);
        }

        if (openingstatusMap[DoorDir.SOUTH] is doorOpeningstatus.OPEN)
        {
            neededDirections.Add(DoorDir.SOUTH);
        }

        if (openingstatusMap[DoorDir.WEST] is doorOpeningstatus.OPEN)
        {
            neededDirections.Add(DoorDir.WEST);
        }

        return neededDirections;
    }


    // Som tar in DoorDir och sätter openingstatus till rätt openingstatus
    private void setReversedDirectionStatus(DoorDir doorDir, doorOpeningstatus openingstatus)
    {
        if (doorDir == DoorDir.NORTH)
        {
            openingstatusMap[DoorDir.SOUTH] = openingstatus;
        }
        else if (doorDir == DoorDir.EAST)
        {
            openingstatusMap[DoorDir.WEST] = openingstatus;
        }
        else if (doorDir == DoorDir.SOUTH)
        {
            openingstatusMap[DoorDir.NORTH] = openingstatus;
        }
        else if (doorDir == DoorDir.WEST)
        {
            openingstatusMap[DoorDir.EAST] = openingstatus;
        }
    }

    Dictionary<DoorDir, doorOpeningstatus> openingstatusMap;

    private void PickCorrectRoomToSpawn(Vector3 roomPos, DoorDir doorDir, DoorDir reversed, DoorDir remainingOne,
        DoorDir remainingTwo)
    {
        openingstatusMap = new Dictionary<DoorDir, doorOpeningstatus>();
        if (Grid.ContainsKey(roomPos + directionModifyer[doorDir]) == false)
        {
            Vector3 newPos = roomPos + directionModifyer[doorDir];
            // Kolla om denna gird pos har någon granne.
            //bool north = false, east = false, south = false, west = false;

            //north = true;
            setReversedDirectionStatus(doorDir, doorOpeningstatus.OPEN); // DoorDir = SOUTH,  

            setCorrectReversedDirectionStatus(doorDir, reversed, newPos);
            setCorrectReversedDirectionStatus(remainingOne, remainingTwo, newPos);
            setCorrectReversedDirectionStatus(remainingTwo, remainingOne, newPos);
        }
    }

    private void setCorrectReversedDirectionStatus(DoorDir doorDir, DoorDir reversed, Vector3 newPos)
    {
        if (Grid.ContainsKey(newPos + directionModifyer[doorDir])) // OM det finns en granne
        {
            Room neighbour = Grid[newPos + directionModifyer[doorDir]]; // Grannen

            if (neighbour.GetDoorDir().Contains(reversed)) // Grannens NORR ÄR NEEDED
            {
                setReversedDirectionStatus(reversed, doorOpeningstatus.OPEN); //SYD ÖPPNING ÄR NEEDED
            }
            else
            {
                setReversedDirectionStatus(reversed,
                    doorOpeningstatus.CLOSED); // ANNARS OM GRANNEN FINNS OCH NORR ÄR STÄNGD ÖPPNA INTE SYD
            }
        }
        else
        {
            setReversedDirectionStatus(reversed, doorOpeningstatus.FREE); // DET FINNS INGEN GRANNE Öppna dörren.
        }
    }
}

public enum doorOpeningstatus
{
    OPEN,
    CLOSED,
    FREE
}
