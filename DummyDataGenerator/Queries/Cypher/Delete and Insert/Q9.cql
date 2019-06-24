MATCH (o:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product)-[:CONSISTS_OF]->(c:Product)
WHERE o.name = 'Reilly - Toy'
AND a.name = 'hacking'
// delete all the nodes and the associated relationships of the activity and the product
DETACH DELETE a, p