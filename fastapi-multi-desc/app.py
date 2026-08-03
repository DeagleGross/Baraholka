"""
Test what FastAPI does when multiple responses are declared for the same status code
with different descriptions. Three scenarios:

1. Two descriptions for same status code (no schema)
2. Two descriptions for same status code, with overlapping content/json schemas
3. Same status code repeated via Python dict literal — does Python dedupe? Last wins?
"""
from fastapi import FastAPI
from pydantic import BaseModel


class FooModel(BaseModel):
    foo: str


class BarModel(BaseModel):
    bar: str


app = FastAPI()


# Scenario 1: framework-level merge. `response_model` provides the implicit success schema
# and description ("Successful Response") for status 200. The `responses` dict ALSO contains
# an entry for status 200 with a different description and schema. Both feed into FastAPI's
# OpenAPI generator, which has to decide what to emit for the single 200 slot.
#
# This is the closest FastAPI analog to ASP.NET Core's multiple `[ProducesResponseType]`
# attributes targeting the same status code: two distinct sources of metadata for the
# same `(status, content-type)` pair, with different descriptions.
@app.get(
    "/scenario1",
    response_model=FooModel,  # implicitly contributes (200, FooModel, "Successful Response")
    responses={
        200: {
            "model": BarModel,
            "description": "Explicit description in responses dict",
        },
    },
)
def scenario1():
    return {"foo": "hi"}


# Scenario 1b: imperatively-built dict with duplicate 200 keys. Python's `dict` cannot hold
# two entries for the same key — the second `d[200] = ...` overwrites the first BEFORE
# FastAPI ever sees the dict. This demonstrates that the FastAPI API surface (`responses:
# Dict[Union[int, str], Dict]`) structurally forecloses the "two descriptions for one
# status" question at the language level, independent of any framework logic.
def build_dup_responses():
    d = {}
    d[200] = {"description": "First description (will be overwritten)"}
    d[200] = {"description": "Second description (survives Python dict dedup)"}
    return d


@app.get("/scenario1b", responses=build_dup_responses())
def scenario1b():
    return {"ok": True}


# Scenario 2: same status code with different schemas, both should appear under one entry.
@app.get(
    "/scenario2",
    responses={
        200: {
            "model": FooModel,
            "description": "Returns Foo",
        },
        # We cannot have two 200 keys; this is the structural limitation we want to demonstrate.
    },
)
def scenario2():
    return {"foo": "hi"}


# Scenario 3: try to abuse string vs int key for the same status — does FastAPI merge
# "200" and 200, or treat them as different?
@app.get(
    "/scenario3",
    responses={
        200: {"description": "Int key 200"},
        "200": {"description": "String key 200"},
    },
)
def scenario3():
    return {"ok": True}


if __name__ == "__main__":
    # Print the generated OpenAPI to stdout so we can inspect it.
    import json
    print(json.dumps(app.openapi(), indent=2))
