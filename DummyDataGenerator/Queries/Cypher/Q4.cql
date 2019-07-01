MATCH path = (p:Product)-[:CONSISTS_OF*]->(c:Product),
path2 = (p)<-[:PRODUCES]-(pa:Activity)<-[:PERFORMS]-(po:Organization)
path3 = (c)<-[:PRODUCES]-(ca:Activity)<-[:PERFORMS]-(co:Organization)
WHERE po.name = 'Top Level Organization Blanda - Bode'
RETURN DISTINCT co.name, length(path) AS depth
ORDER BY depth