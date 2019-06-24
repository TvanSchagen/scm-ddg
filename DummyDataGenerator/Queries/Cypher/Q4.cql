MATCH path = (p:Product)-[:CONSISTS_OF*]->(c:Product),
path2 = (c)<-[:PRODUCES]-(ca:Activity)<-[:PERFORMS]-(co:Organization)
WHERE p.name = 'Top Level Product Awesome Steel Car'
RETURN DISTINCT co.name, length(path) AS depth
ORDER BY depth