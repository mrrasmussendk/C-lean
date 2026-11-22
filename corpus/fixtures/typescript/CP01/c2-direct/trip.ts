export class Engine {
  run(): number {
    return 1;
  }
}

export class Car {
  engine = new Engine();

  getEngine(): Engine {
    return this.engine;
  }
}

export class Garage {
  car = new Car();

  getCar(): Car {
    return this.car;
  }
}

export class Street {
  garage = new Garage();

  getGarage(): Garage {
    return this.garage;
  }
}

export function start(engine: Engine): number {
  return engine.run();
}
