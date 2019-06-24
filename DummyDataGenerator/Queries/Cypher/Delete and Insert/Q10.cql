MATCH (o:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product)-[rel:CONSISTS_OF]->(c:Product)
// match the desired product, for instance by id
WHERE ID(p) = 4448
// create the product that has to sit in between
MERGE (p2:Product {name: 'New Product'})
// create the new relation based on the old activity
MERGE (a)-[:PRODUCES]->(p2)
// create new relationships for all child nodes
MERGE (p2)-[:CONSISTS_OF]->(c)
// delete the original relationship
DELETE rel