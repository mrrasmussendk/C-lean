export interface Throw {
  aim: number;
  power: number;
  drift: number;
}

export function speed(ball: Throw): number {
  return ball.power - ball.aim * ball.drift;
}

export function curve(ball: Throw): number {
  return ball.aim + ball.drift / ball.power;
}

export function describe(ball: Throw): string {
  return `${ball.aim}/${ball.power}/${ball.drift}`;
}
