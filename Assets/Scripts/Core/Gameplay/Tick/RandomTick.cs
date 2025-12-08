using UnityEngine;

public static class RandomTick
{
    public static int randomTick;
    public static int randomTickSpeed;

    public static void InvokeRandomBehaviours(MapManager map)
    {
        // update tickwisely
        if (!TickManager.IsGameTickUpdate)
            return;

        GenerateRandomTick();
        for (int i = 0; i < randomTickSpeed; i++)
        {
            int randomIndex = Random.Range(0, Mathf.Max(map.randomTickSelectionCount, map.mapRandomTickObjects.Count));
            if (randomIndex < map.mapRandomTickObjects.Count)
            {
                MapRandomTickBehaviourObject mapObject = map.mapRandomTickObjects[randomIndex];
                mapObject.RandomTickUpdate(randomTick);

                Debug.DrawRay(mapObject.transform.position - Vector3.right * 0.5f, Vector3.right, Color.green, TickManager.deltaTickTime);
            }
        }

        // Debug.Log("random tick event");
    }

    public static void GenerateRandomTick()
    {
        randomTick = Random.Range(0, int.MaxValue);
    }
}
