using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

public class RollerAgent : Agent
{
    // Объявляем поля класса

    // Лист блоков(звеньев) змейки
    public List<GameObject> Blocks = new List<GameObject>();
    // Объект префаба блоков
    public GameObject prefab;
    // Позиция головы змейки на предыдущей итерации
    private Vector3 lastPosition;
    // Компонент Rigibody головы змейки
    Rigidbody rBody;
    // Компонент Transform цели змейки(кубика)
    public Transform Target;

    // При старте программы объявляем компонент Rigibody 
    // И первичное положение змейки
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        lastPosition = rBody.position;
    }

    // Метод, вызываемый при старте каждого эпизода обучения
    public override void OnEpisodeBegin()
    {
        // Если змейка упала с платформы, ставим ещё на середину платформы
        if (this.transform.localPosition.y < 0 )
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }
        // Объявляем рандомное положение цели змейки
        Target.localPosition = new Vector3(Random.value * 8 - 4, 0.5f, Random.value * 8 - 4);
    }

    // Метод, объявляющий сенсоры(параметы, которыми владеет модель для обучения)
    public override void CollectObservations(VectorSensor sensor)
    {
        // Позиция головы змейки
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        // x координата вектора скорости головы змейки
        sensor.AddObservation(rBody.velocity.x);
        // z координата вектора скорости головы змейки
        sensor.AddObservation(rBody.velocity.z);
        // Позиции всех звеньев змейки
        foreach(var block in Blocks)
        {
            sensor.AddObservation(block.transform.localPosition);
        }
    }
    // Коэффициент кратности добавления силы к голове змейки
    public float forceMultiplier = 10;
    // Метод, используемый для действий с моделью, с передаваемыми параметрами actionByffer
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Добавляем к голове змейки вектор силы, полученный из переданных параметов
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        // Вычисляем дистанцию до цели
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        // Обновляем позиции звеньев змейки
        UpdateVelocity();
        // Выдаем "награду" модели за достигнутую цель
        // (расстояние до цели меньше 1.46f)
        if (distanceToTarget < 1.46f)
        {
            SetReward(1.0f);
            CreateNewBlock();
            EndEpisode();
        }
        // (В цепи змейки 10 звеньев)
        else if (Blocks.Count >= 10)
        {
            SetReward(3.0f);
            foreach (var block in Blocks)
                Destroy(block);
            Blocks.Clear();
            EndEpisode();
            
        }
        // Заканчиваем эпизод и удаляем звенья, если змейка упала
        else if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
            foreach (var block in Blocks)
                Destroy(block);
            Blocks.Clear();

        }
    }
    // Создание нового звена змейки, как клона префаба
    public void CreateNewBlock()
    {
        var position = Blocks.Count == 0 ? transform.position : Blocks[Blocks.Count - 1].transform.position;
        var rotation = Blocks.Count == 0 ? transform.rotation : Blocks[Blocks.Count - 1].transform.rotation;
        var clone = Instantiate(prefab, position, rotation);
        Blocks.Add(clone);
    }

    // Метод, описывающий логику взаимодействия головы змейки с её звеньями
    public void OnTriggerEnter(Collider other)
    {
        // Если змейка касается одного из звеньев, удаляем блоки и заканчиваем эпизод
        if (Blocks.Skip(1).Select(p => p.GetComponent<Collider>()).Contains(other))
        {
            foreach (var block in Blocks)
                Destroy(block);
            Blocks.Clear();
            EndEpisode();
        }
        
    }

    // Обновляем векторы скорости звеньев змейки относительно вектора скорости головы
    public void UpdateVelocity()
    {
        
        for (var block = 0; block<Blocks.Count; block++)
        {
            if (block == 0)
            {
                var velocity = transform.position - Blocks[block].transform.position;
                velocity.Normalize();
                Blocks[block].GetComponent<Rigidbody>().velocity = velocity*7;
            }
            else
            {
                var velocity = Blocks[block-1].transform.position - Blocks[block].transform.position;
                velocity.Normalize();
                Blocks[block].GetComponent<Rigidbody>().velocity = velocity*7;
            }
        }
    }
}
