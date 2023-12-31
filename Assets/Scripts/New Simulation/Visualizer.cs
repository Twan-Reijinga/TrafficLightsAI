using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimulationAPI;
using System.Linq;
using System.Runtime.InteropServices;

public class Visualizer : MonoBehaviour
{
    public GameObject Car;
    public Transform CarParent;
    public Transform trafficLights;
    public Dictionary<int, List<Car>> idToCars;

    public G_sceneState state1 = null;
    public G_sceneState state2 = null;
    public bool interpolate = false;
    public float timeBetweenVisualisations;

    [Range(0.0f, 1.0f)]
    public float t = 0f;
    void Update()
    {
        if (state1 != null && state2 != null)
        {
            if (interpolate)
            {
                DrawState(SceneLerp(state1, state2, t));
                t += Time.deltaTime / timeBetweenVisualisations;
            }
            else
            {
                DrawState(state1);
            }
        }
    }

    public void DrawState(G_sceneState sceneState)
    {
        foreach (Car car in sceneState.cars)
        {
            bool exists = false;
            foreach (Transform gCar in CarParent.transform)
            {
                if (gCar.name == car.UUID.ToString())
                {
                    gCar.GetComponent<SceneCar>().isInScene = true;
                    gCar.GetComponent<SceneCar>().car = car;
                    gCar.position = new Vector3(car.pos.x, 0, car.pos.y);
                    gCar.rotation = Quaternion.Euler(new Vector3(0, car.orientation, 0));
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                GameObject newCar = Instantiate(Car, new Vector3(car.pos.x, 0, car.pos.y), Quaternion.Euler(new Vector3(0, car.orientation, 0)), CarParent);
                SceneCar sc = newCar.AddComponent<SceneCar>();
                sc.car = car;
                newCar.name = car.UUID.ToString();
                newCar.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
        }

        for (int i = CarParent.childCount - 1; i >= 0; i--)
        {
            for (int j = 0; j < sceneState.deletedCars.Count; j++)
            {
                if (CarParent.GetChild(i).name == sceneState.deletedCars[j].ToString())
                {
                    Destroy(CarParent.GetChild(i).gameObject);
                }
            }
        }

        List<Transform> intersections = new List<Transform> { trafficLights.GetChild(0), trafficLights.GetChild(1) };

        for (int i = 0; i < 8; i++)
        {
            int dualI = Mathf.FloorToInt(i / 2);
            int otherI = i % 2 == 0 ? 0 : 1;

            intersections[0].GetChild(dualI).GetChild(otherI).GetComponent<TrafficLight>().isGreen = sceneState.lights.cross1[i];
            intersections[1].GetChild(dualI).GetChild(otherI).GetComponent<TrafficLight>().isGreen = sceneState.lights.cross2[i];
        }
    }

    public void SetVisuals(G_sceneState state)
    {
        t = 0;
        state1 = state2;
        state2 = state;
        if (state1 != null)
        {
            idToCars = GetPairedInts(state1.cars, state2.cars);
        }
    }

    public G_sceneState SceneLerp(G_sceneState state1, G_sceneState state2, float t)
    {
        G_sceneState inter = new G_sceneState();
        inter.lights = state1.lights;
        foreach (Car car in state1.cars)
        {
            Car startCar = idToCars?[car.UUID][0];
            Car endCar = idToCars?[car.UUID][1];

            if (startCar != null && endCar != null)
            {
                Car newCar = new Car(startCar);

                newCar.pos = SimulationAPI.Vector2.Lerp(startCar.pos, endCar.pos, t);
                newCar.orientation = Mathf.Lerp(startCar.orientation, endCar.orientation, t);
                newCar.UUID = car.UUID;

                inter.cars.Add(newCar);
            }
            else
            {
                Debug.Log("FUU-");
            }

        }
        return inter;
    }

    static int sortByUUID(Car c1, Car c2)
    {
        return c1.UUID.CompareTo(c2.UUID);
    }

    public Dictionary<int, List<Car>> GetPairedInts(List<Car> c1, List<Car> c2)
    {
        Dictionary<int, List<Car>> idToCarsMap = new Dictionary<int, List<Car>>();

        foreach (Car car in c1)
        {
            if (idToCarsMap.ContainsKey(car.UUID))
            {
                Debug.Log("WHAT THE FU-");
            }
            int c2index = getIndexFromUUID(c2, car.UUID);
            if (c2index != -1)
            {
                idToCarsMap[car.UUID] = new List<Car>
                {
                    car,
                    c2[getIndexFromUUID(c2, car.UUID)]
                };
            }
        }
        return idToCarsMap;
    }

    int getIndexFromUUID(List<Car> cars, int UUID)
    {
        return cars.FindIndex(car => car.UUID == UUID);
    }
}
