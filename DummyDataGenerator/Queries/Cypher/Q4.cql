MATCH (o:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product)-[r:CONSISTS_OF*]->(c:Product)<-[:PRODUCES]-(ca:Activity)<-[:PERFORMS]-(co:Organization)
WHERE p.name = 'Top Level Product Awesome Steel Car'
RETURN DISTINCT co.name, size(r) AS depth
ORDER BY depth