using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

public class RollerAgent : Agent
{
    // ��������� ���� ������

    // ���� ������(�������) ������
    public List<GameObject> Blocks = new List<GameObject>();
    // ������ ������� ������
    public GameObject prefab;
    // ������� ������ ������ �� ���������� ��������
    private Vector3 lastPosition;
    // ��������� Rigibody ������ ������
    Rigidbody rBody;
    // ��������� Transform ���� ������(������)
    public Transform Target;

    // ��� ������ ��������� ��������� ��������� Rigibody 
    // � ��������� ��������� ������
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        lastPosition = rBody.position;
    }

    // �����, ���������� ��� ������ ������� ������� ��������
    public override void OnEpisodeBegin()
    {
        // ���� ������ ����� � ���������, ������ ��� �� �������� ���������
        if (this.transform.localPosition.y < 0 )
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }
        // ��������� ��������� ��������� ���� ������
        Target.localPosition = new Vector3(Random.value * 8 - 4, 0.5f, Random.value * 8 - 4);
    }

    // �����, ����������� �������(��������, �������� ������� ������ ��� ��������)
    public override void CollectObservations(VectorSensor sensor)
    {
        // ������� ������ ������
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        // x ���������� ������� �������� ������ ������
        sensor.AddObservation(rBody.velocity.x);
        // z ���������� ������� �������� ������ ������
        sensor.AddObservation(rBody.velocity.z);
        // ������� ���� ������� ������
        foreach(var block in Blocks)
        {
            sensor.AddObservation(block.transform.localPosition);
        }
    }
    // ����������� ��������� ���������� ���� � ������ ������
    public float forceMultiplier = 10;
    // �����, ������������ ��� �������� � �������, � ������������� ����������� actionByffer
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // ��������� � ������ ������ ������ ����, ���������� �� ���������� ���������
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        // ��������� ��������� �� ����
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        // ��������� ������� ������� ������
        UpdateVelocity();
        // ������ "�������" ������ �� ����������� ����
        // (���������� �� ���� ������ 1.46f)
        if (distanceToTarget < 1.46f)
        {
            SetReward(1.0f);
            CreateNewBlock();
            EndEpisode();
        }
        // (� ���� ������ 10 �������)
        else if (Blocks.Count >= 10)
        {
            SetReward(3.0f);
            foreach (var block in Blocks)
                Destroy(block);
            Blocks.Clear();
            EndEpisode();
            
        }
        // ����������� ������ � ������� ������, ���� ������ �����
        else if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
            foreach (var block in Blocks)
                Destroy(block);
            Blocks.Clear();

        }
    }
    // �������� ������ ����� ������, ��� ����� �������
    public void CreateNewBlock()
    {
        var position = Blocks.Count == 0 ? transform.position : Blocks[Blocks.Count - 1].transform.position;
        var rotation = Blocks.Count == 0 ? transform.rotation : Blocks[Blocks.Count - 1].transform.rotation;
        var clone = Instantiate(prefab, position, rotation);
        Blocks.Add(clone);
    }

    // �����, ����������� ������ �������������� ������ ������ � � ��������
    public void OnTriggerEnter(Collider other)
    {
        // ���� ������ �������� ������ �� �������, ������� ����� � ����������� ������
        if (Blocks.Skip(1).Select(p => p.GetComponent<Collider>()).Contains(other))
        {
            foreach (var block in Blocks)
                Destroy(block);
            Blocks.Clear();
            EndEpisode();
        }
        
    }

    // ��������� ������� �������� ������� ������ ������������ ������� �������� ������
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
