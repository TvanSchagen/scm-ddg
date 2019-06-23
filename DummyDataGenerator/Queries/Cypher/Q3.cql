MATCH (po:Organization)-[:PERFORMS]->(a:Activity)-[:PRODUCES]->(p:Product)-[:CONSISTS_OF*]->(c:Product)<-[PRODUCES]-(ca:Activity)<-[:PERFORMS]-(co:Organization),
(ca)-[:LOCATED_AT]->(l:Location)
WHERE po.name = 'Top Level Organization Blanda - Bode'
AND toLower(c.name) CONTAINS 'concrete'
RETURN p.name, ID(p), c.name, ID(c), ID(co), co.name, ID(l), l.country