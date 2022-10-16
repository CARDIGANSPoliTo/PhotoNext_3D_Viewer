using ChartAndGraph;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGraphScript : MonoBehaviour {
    public Material lineMaterial, pointMaterial, fillMaterial;
    public double lineThickness = 2.0, pointSize = 5.0;
    public bool stertchFill = false;

    float timer = 0.0f;

    // Start is called before the first frame update
    void Start() {
        GraphChartBase graph = GetComponent<GraphChartBase>();
        MaterialTiling lineTiling = new MaterialTiling(true, 10);
        if (graph != null) {
            graph.DataSource.StartBatch();
            for(int i=0; i < 64; i++) {
                Color rand = Random.ColorHSV();
                Debug.Log($"{i} - Random color {rand}");
                Material newmat = new Material(lineMaterial);
                newmat.color = rand;
                //fillMaterial.color = new Color(Random.Range(0, 1), Random.Range(0, 1), Random.Range(0, 1)); 
                graph.DataSource.AddCategory(i.ToString(), newmat, lineThickness, lineTiling, fillMaterial, stertchFill, pointMaterial, pointSize);
            }
            graph.DataSource.EndBatch();
        }
        //StartCoroutine("updateGraph");
    }

    //// Update is called once per frame
    //IEnumerator updateGraph() {
    //    while (true) {
    //        yield return new WaitForSeconds(1);
    //        GraphChartBase graph = GetComponent<GraphChartBase>();

    //        if (graph != null) {
    //            graph.DataSource.StartBatch();
    //            for (int i = 0; i < 4; i++) {
    //                graph.DataSource.AddPointToCategory(i.ToString(), Time.frameCount, Random.value * 10f + 20f);
    //            }
    //            graph.DataSource.EndBatch();
    //        }
    //    }
    //}

    private void Update () {
        timer += Time.deltaTime;
        if (timer > 0.08f) {
            timer = 0.0f;
            GraphChartBase graph = GetComponent<GraphChartBase>();

            if (graph != null) {
                graph.DataSource.StartBatch();
                for (int i = 0; i < 64; i++) {
                    graph.DataSource.AddPointToCategory(i.ToString(), Time.frameCount, Random.Range(1520, 1580));
                }
                graph.DataSource.EndBatch();
            }
        }
    }

}
