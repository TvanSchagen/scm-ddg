MATCH (po:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product)-[:CONSISTS_OF*1..3]->(c:Product)<-[PRODUCES]-(ca:Activity)<-[:PERFORMS]-(co:Organization),
(a)-[:LOCATED_AT]->(l:Location),
(ca)-[:LOCATED_AT]->(cl:Location)
WHERE p.created > datetime('2017-01-01')
AND po.number_of_employees > 1000
AND l.country IN ['Guam', 'Georgia', 'Dominica']
RETURN p.ean, p.name, po.name, l.country, c.ean, c.name, cl.country